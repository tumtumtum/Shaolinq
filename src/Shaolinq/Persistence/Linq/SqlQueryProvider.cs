// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;
using Shaolinq.Logging;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryProvider
		: ReusableQueryProvider
	{
		private readonly int ProjectorCacheMaxLimit = 512;
        protected static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }

		public static Dictionary<Type, Func<SqlQueryProvider, Expression, IQueryable>> createQueryCache = new Dictionary<Type, Func<SqlQueryProvider, Expression, IQueryable>>();

		public static IQueryable CreateQuery(Type elementType, SqlQueryProvider provider, Expression expression)
		{
			Func<SqlQueryProvider, Expression, IQueryable> func;

			if (!createQueryCache.TryGetValue(elementType, out func))
			{
				var providerParam = Expression.Parameter(typeof(SqlQueryProvider));
				var expressionParam = Expression.Parameter(typeof(Expression));

				func = Expression.Lambda<Func<SqlQueryProvider, Expression, IQueryable>>(Expression.New
				(
					TypeUtils.GetConstructor(() => new SqlQueryable<object>(null, null)).GetConstructorOnTypeReplacingTypeGenericArgs(elementType),
					providerParam,
					expressionParam
				), providerParam, expressionParam).Compile();

				var newCreateQueryCache = new Dictionary<Type, Func<SqlQueryProvider, Expression, IQueryable>>(createQueryCache) { [elementType] = func };

				createQueryCache = newCreateQueryCache;
			}

			return func(provider, expression);
		}
		
		public override string GetQueryText(Expression expression)
		{
			SqlProjectionExpression projectionExpression;

			if (this.RelatedDataAccessObjectContext == null)
			{
				projectionExpression = (SqlProjectionExpression)(QueryBinder.Bind(this.DataAccessModel, expression, null, null));
			}
			else
			{
				projectionExpression = (SqlProjectionExpression)(QueryBinder.Bind(this.DataAccessModel, expression, this.RelatedDataAccessObjectContext.ElementType, this.RelatedDataAccessObjectContext.ExtraCondition));
			}

			var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionExpression);

			return result.CommandText;
		}

		protected override IQueryable CreateQuery(Type elementType, Expression expression)
		{
			return CreateQuery(elementType, this, expression);
		}

		public SqlQueryProvider(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext)
		{
			this.DataAccessModel = dataAccessModel;
			this.SqlDatabaseContext = sqlDatabaseContext;
		}
		
		public override IQueryable<T> CreateQuery<T>(Expression expression)
		{
			return new SqlQueryable<T>(this, expression);
		}

		public override object Execute(Expression expression)
		{
			return this.Execute<object>(expression);
		}

		public override T Execute<T>(Expression expression)
		{
			return this.BuildExecution(expression).Evaluate<T>();
		}

		public override IEnumerable<T> GetEnumerable<T>(Expression expression)
		{
			return this.BuildExecution(expression).Evaluate<IEnumerable<T>>();
		}

		public static Expression Optimize(DataAccessModel dataAccessModel, Expression expression, Type typeForEnums, bool simplerPartialVal = true)
		{
			expression = SqlNullComparisonCoalescer.Coalesce(expression);
			expression = SqlJoinConditionExpander.Expand(expression);
			expression = SqlObjectOperandComparisonExpander.Expand(expression);
			expression = SqlEnumTypeNormalizer.Normalize(expression, typeForEnums);
			expression = SqlGroupByCollator.Collate(expression);
			expression = SqlAggregateSubqueryRewriter.Rewrite(expression);
			expression = SqlUnusedColumnRemover.Remove(expression);
			expression = SqlRedundantColumnRemover.Remove(expression);
			expression = SqlRedundantSubqueryRemover.Remove(expression);
			expression = SqlFunctionCoalescer.Coalesce(expression);
			expression = SqlExistsSubqueryOptimizer.Optimize(expression);
			expression = SqlRedundantBinaryExpressionsRemover.Remove(expression);
			expression = SqlCrossJoinRewriter.Rewrite(expression);
			expression = Evaluator.PartialEval(expression);
			expression = SqlRedundantFunctionCallRemover.Remove(expression);
			expression = SqlConditionalEliminator.Eliminate(expression);
			expression = SqlExpressionCollectionOperationsExpander.Expand(expression);
			expression = SqlSubCollectionOrderByAmender.Amend(dataAccessModel, expression);
			expression = SqlOrderByRewriter.Rewrite(expression);

			var rewritten = SqlCrossApplyRewriter.Rewrite(expression);

			if (rewritten != expression)
			{
				expression = rewritten;
				expression = SqlUnusedColumnRemover.Remove(expression);
				expression = SqlRedundantColumnRemover.Remove(expression);
				expression = SqlRedundantSubqueryRemover.Remove(expression);
				expression = SqlOrderByRewriter.Rewrite(expression);
			}

			return expression;
		}

		public ExecutionBuildResult BuildExecution(Expression expression)
		{
			var projectionExpression = expression as SqlProjectionExpression;
			
			if (projectionExpression == null)
			{
				expression = Evaluator.PartialEval(expression);
				expression = QueryBinder.Bind(this.DataAccessModel, expression, this.RelatedDataAccessObjectContext?.ElementType, this.RelatedDataAccessObjectContext?.ExtraCondition);

				projectionExpression = (SqlProjectionExpression)Optimize(this.DataAccessModel, expression, this.SqlDatabaseContext.SqlDataTypeProvider.GetTypeForEnums(), true);
			}

			var placeholderValues = SqlConstantPlaceholderValuesCollector.CollectValues(expression);
			var columns = projectionExpression.Select.Columns.Select(c => c.Name);
			var projector = ProjectionBuilder.Build(this.DataAccessModel, this.SqlDatabaseContext, this, projectionExpression.Projector, new ProjectionBuilderScope(columns.ToArray()));

			return this.BuildExecution(projectionExpression, projector, placeholderValues);
		}

        public ExecutionBuildResult BuildExecution(SqlProjectionExpression projectionExpression, LambdaExpression projector, object[] placeholderValues)
		{
			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionExpression);

			ProjectorCacheInfo cacheInfo;
			
			var formatResultsParam = Expression.Parameter(typeof(SqlQueryFormatResult));
			var placeholderValuesParam = Expression.Parameter(typeof(object[]));
			var projectionLambda = projector;

			var elementType = projectionLambda.ReturnType;

			Expression executor;

			if (elementType.IsDataAccessObjectType())
			{
				var concreteElementType = this.DataAccessModel.GetConcreteTypeFromDefinitionType(elementType);

				if (this.RelatedDataAccessObjectContext == null)
				{
					var constructor = typeof(DataAccessObjectProjector<,>).MakeGenericType(elementType, concreteElementType).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

					executor = Expression.New
					(
						constructor,
						Expression.Constant(this),
						Expression.Constant(this.DataAccessModel),
						Expression.Constant(this.SqlDatabaseContext),
						Expression.Constant(this.RelatedDataAccessObjectContext, typeof(IRelatedDataAccessObjectContext)),
						formatResultsParam,
						placeholderValuesParam,
						projectionLambda
					);
				}
				else
				{
					var constructor = typeof(RelatedDataAccessObjectProjector<,>).MakeGenericType(elementType, concreteElementType).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

					executor = Expression.New
					(
						constructor,
						Expression.Constant(this),
						Expression.Constant(this.DataAccessModel),
						Expression.Constant(this.SqlDatabaseContext),
						Expression.Constant(this.RelatedDataAccessObjectContext, typeof(IRelatedDataAccessObjectContext)),
						formatResultsParam,
						placeholderValuesParam,
						projectionLambda
					);
				}
			}
			else
			{
				var constructor = typeof(ObjectProjector<,>).MakeGenericType(projectionLambda.ReturnType, projectionLambda.ReturnType).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

				executor = Expression.New
				(
					constructor,
					Expression.Constant(this),
					Expression.Constant(this.DataAccessModel),
					Expression.Constant(this.SqlDatabaseContext),
					Expression.Constant(this.RelatedDataAccessObjectContext, typeof(IRelatedDataAccessObjectContext)),
					formatResultsParam,
					placeholderValuesParam,
					projectionLambda
				);
			}

	        if (projectionExpression.Aggregator != null)
	        {
		        var newBody = SqlConstantPlaceholderReplacer.Replace(projectionExpression.Aggregator.Body, placeholderValuesParam);

		        executor = SqlExpressionReplacer.Replace(newBody, projectionExpression.Aggregator.Parameters[0], executor);
	        }

	        projector = Expression.Lambda(executor, formatResultsParam, placeholderValuesParam);
            
			var key = new ProjectorCacheKey(formatResult.CommandText, projector);
			var projectorCache = this.SqlDatabaseContext.projectorCache;

			if (projectorCache.TryGetValue(key, out cacheInfo))
			{
				return new ExecutionBuildResult(formatResult, cacheInfo.projector, placeholderValues);
			}

			cacheInfo.projector = projector.Compile();

			var oldCache = this.SqlDatabaseContext.projectorCache;
			Dictionary<ProjectorCacheKey, ProjectorCacheInfo> newCache;

	        if (oldCache.Count >= ProjectorCacheMaxLimit)
	        {
		        Logger.Error(() => $"ProjectorCache has been flushed because it overflowed with a size of {ProjectorCacheMaxLimit}\n\n{formatResult.CommandText}\n\n{projector}");

				newCache = new Dictionary<ProjectorCacheKey, ProjectorCacheInfo>(ProjectorCacheEqualityComparer.Default);

		        foreach (var value in oldCache.Take(oldCache.Count / 3))
		        {
			        newCache[value.Key] = value.Value;
		        }
			}
	        else
	        {
				newCache = new Dictionary<ProjectorCacheKey, ProjectorCacheInfo>(oldCache, ProjectorCacheEqualityComparer.Default);
			}

			Logger.Debug(() => $"Caching projection for query:\n{formatResult.CommandText}\n\nprojector:\n{projector}");
	        
			newCache[key] = cacheInfo;

			this.SqlDatabaseContext.projectorCache = newCache;

			Logger.Debug(() => $"Projector Cache Size: {newCache.Count}");

			return new ExecutionBuildResult(formatResult, cacheInfo.projector, placeholderValues);
		}
	}
}
