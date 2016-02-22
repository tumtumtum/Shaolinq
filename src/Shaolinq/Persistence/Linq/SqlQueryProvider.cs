// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly Random random = new Random();
        private readonly int ProjectorCacheMaxLimit = 512;
        protected static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }

		public static Dictionary<RuntimeTypeHandle, Func<SqlQueryProvider, Expression, IQueryable>> createQueryCache = new Dictionary<RuntimeTypeHandle, Func<SqlQueryProvider, Expression, IQueryable>>();

		public static IQueryable CreateQuery(Type elementType, SqlQueryProvider provider, Expression expression)
		{
			Func<SqlQueryProvider, Expression, IQueryable> func;

			if (!createQueryCache.TryGetValue(elementType.TypeHandle, out func))
			{
				var providerParam = Expression.Parameter(typeof(SqlQueryProvider));
				var expressionParam = Expression.Parameter(typeof(Expression));

				func = Expression.Lambda<Func<SqlQueryProvider, Expression, IQueryable>>(Expression.New
				(
					TypeUtils.GetConstructor(() => new SqlQueryable<object>(null, null)).GetConstructorOnTypeReplacingTypeGenericArgs(elementType),
					providerParam,
					expressionParam
				), providerParam, expressionParam).Compile();

				var newCreateQueryCache = new Dictionary<RuntimeTypeHandle, Func<SqlQueryProvider, Expression, IQueryable>>(createQueryCache) { [elementType.TypeHandle] = func };

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

			var optimisedExpression = Optimize(this.DataAccessModel, this.SqlDatabaseContext, projectionExpression);

			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(optimisedExpression);
			var sql = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(formatResult.CommandText, c =>
			{
				var index = c.IndexOf(Char.IsDigit);

				if (index < 0)
				{
					return "(?!)";
				}

				index = int.Parse(c.Substring(index));

				return formatResult.ParameterValues[index].Value;
			});

			return sql;
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

        public override Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken)
        {
            return this.BuildExecution(expression).EvaluateAsync<Task<T>>(cancellationToken);
        }

        public override IEnumerable<T> GetEnumerable<T>(Expression expression)
		{
			return this.BuildExecution(expression).Evaluate<IEnumerable<T>>();
        }

        public override IAsyncEnumerable<T> GetAsyncEnumerable<T>(Expression expression)
		{
            return this.BuildExecution(expression).EvaluateAsync<IAsyncEnumerable<T>>(CancellationToken.None);
        }

        public static Expression Optimize(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, Expression expression)
		{
			expression = SqlNullComparisonCoalescer.Coalesce(expression);
			expression = SqlTupleOrAnonymousTypeComparisonExpander.Expand(expression);
			expression = SqlObjectOperandComparisonExpander.Expand(expression);
			expression = SqlEnumTypeNormalizer.Normalize(expression, sqlDatabaseContext.SqlDataTypeProvider.GetTypeForEnums());
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

            expression = SqlDeleteNormalizer.Normalize(expression);

			return expression;
		}

		public ExecutionBuildResult BuildExecution(Expression expression)
		{
			var projectionExpression = expression as SqlProjectionExpression;
			
			if (projectionExpression == null)
			{
				expression = Evaluator.PartialEval(expression);
				expression = QueryBinder.Bind(this.DataAccessModel, expression, this.RelatedDataAccessObjectContext?.ElementType, this.RelatedDataAccessObjectContext?.ExtraCondition);

				projectionExpression = (SqlProjectionExpression)Optimize(this.DataAccessModel, this.SqlDatabaseContext, expression);
			}

			var placeholderValues = SqlConstantPlaceholderValuesCollector.CollectValues(expression);
			var columns = projectionExpression.Select.Columns.Select(c => c.Name);
			var projector = ProjectionBuilder.Build(this.DataAccessModel, this.SqlDatabaseContext, this, projectionExpression.Projector, new ProjectionBuilderScope(columns.ToArray()));

			return this.BuildExecution(projectionExpression, projector, placeholderValues);
		}

	    public ExecutionBuildResult BuildExecution(SqlProjectionExpression projectionExpression, LambdaExpression projector, object[] placeholderValues, bool replaceValuesForFormat = false)
        {
			ProjectorCacheInfo cacheInfo;

			var projectionForFormat = replaceValuesForFormat ? (SqlProjectionExpression)SqlConstantPlaceholderReplacer.Replace(projectionExpression, placeholderValues) : projectionExpression;
			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionForFormat);

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

	        var asyncExecutor = executor;
			var cancellationToken = Expression.Parameter(typeof(CancellationToken));

			if (projectionExpression.Aggregator != null)
	        {
		        var originalExecutor = executor;
                var newBody = SqlConstantPlaceholderReplacer.Replace(projectionExpression.Aggregator.Body, placeholderValuesParam);
                executor = SqlExpressionReplacer.Replace(newBody, projectionExpression.Aggregator.Parameters[0], originalExecutor);

		        newBody = ProjectionAsyncRewriter.Rewrite(newBody, cancellationToken);
				asyncExecutor = SqlExpressionReplacer.Replace(newBody, projectionExpression.Aggregator.Parameters[0], originalExecutor);
	        }

	        projector = Expression.Lambda(executor, formatResultsParam, placeholderValuesParam);
            
			var key = new ProjectorCacheKey(formatResult.CommandText, projector);
			var projectorCache = this.SqlDatabaseContext.projectorCache;

			if (projectorCache.TryGetValue(key, out cacheInfo))
			{
				return new ExecutionBuildResult(formatResult, cacheInfo.projector, cacheInfo.asyncProjector, placeholderValues);
			}

			projector = Expression.Lambda(executor, formatResultsParam, placeholderValuesParam);
			var asyncProjector = Expression.Lambda(asyncExecutor, formatResultsParam, placeholderValuesParam, cancellationToken);

			cacheInfo.projector = projector.Compile();
            cacheInfo.asyncProjector = asyncProjector.Compile();

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

	            newCache[key] = cacheInfo;
	        }
	        else
	        {
	            var i = 0;

	            while (true)
	            {
	                oldCache = this.SqlDatabaseContext.projectorCache;
	                newCache = new Dictionary<ProjectorCacheKey, ProjectorCacheInfo>(oldCache, ProjectorCacheEqualityComparer.Default);

	                if (oldCache.Count == newCache.Count)
	                {
                        break;
	                }

	                if (i++ > 10)
	                {
                        break;
	                }

	                Thread.Sleep(random.Next(25));
	            }
                
	            newCache[key] = cacheInfo;
	        }

			this.SqlDatabaseContext.projectorCache = newCache;

            Logger.Debug(() => $"Cached projection for query:\n{formatResult.CommandText}\n\nprojector:\n{projector}");
            Logger.Debug(() => $"Projector Cache Size: {newCache.Count}");

			return new ExecutionBuildResult(formatResult, cacheInfo.projector, cacheInfo.asyncProjector, placeholderValues);
		}
	}
}
;