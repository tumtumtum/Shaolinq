// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryProvider
		: ReusableQueryProvider
	{
        private readonly Random random = new Random();
        private readonly int ProjectorCacheMaxLimit = 512;
		private readonly int ProjectionExpressionCacheMaxLimit = 512;
		protected static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
		protected static readonly ILog ProjectionCacheLogger = LogProvider.GetLogger("Shaolinq.ProjectionCache");
		protected static readonly ILog ProjectionExpressionCacheLogger = LogProvider.GetLogger("Shaolinq.ProjectionExpressionCache");

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
			expression = (SqlProjectionExpression)this.Bind(expression);
			var projectionExpression = Optimize(this.DataAccessModel, this.SqlDatabaseContext, expression);
			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionExpression);

			var sql = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(formatResult.CommandText, c =>
			{
				var index = c.IndexOf(char.IsDigit);

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
			expression = SqlGroupByCollator.Collate(expression);
			expression = SqlAggregateSubqueryRewriter.Rewrite(expression);
			expression = SqlUnusedColumnRemover.Remove(expression);
			expression = SqlRedundantColumnRemover.Remove(expression);
			expression = SqlRedundantSubqueryRemover.Remove(expression);
			expression = SqlFunctionCoalescer.Coalesce(expression);
			expression = SqlExistsSubqueryOptimizer.Optimize(expression);
			expression = SqlRedundantBinaryExpressionsRemover.Remove(expression);
			expression = SqlCrossJoinRewriter.Rewrite(expression);
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

		private Expression Bind(Expression expression)
		{
			expression = Evaluator.PartialEval(expression);
			expression = QueryBinder.Bind(this.DataAccessModel, expression);
			expression = SqlEnumTypeNormalizer.Normalize(expression, this.SqlDatabaseContext.SqlDataTypeProvider.GetTypeForEnums());
			expression = Evaluator.PartialEval(expression);

			return expression;
		}

		public ExecutionBuildResult BuildExecution(Expression expression)
		{
			var skipFormatResultSubstitution = false;
			SqlQueryFormatResult formatResult = null;
			var projectionExpression = expression as SqlProjectionExpression;
			object[] placeholderValues = null;

			if (projectionExpression == null)
			{
				ProjectorExpressionCacheInfo cacheInfo;
				var key = new ExpressionCacheKey(expression);
				
				if (!this.SqlDatabaseContext.projectionExpressionCache.TryGetValue(key, out cacheInfo))
				{
					expression = this.Bind(expression);
					placeholderValues = SqlConstantPlaceholderValuesCollector.CollectValues(expression);
					
					projectionExpression = (SqlProjectionExpression)Optimize(this.DataAccessModel, this.SqlDatabaseContext, expression);

					var oldCache = this.SqlDatabaseContext.projectionExpressionCache;
					formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionExpression);

					var formatResultForCache = formatResult;

					if (formatResult.ParameterIndexToPlaceholderIndexes != null)
					{
						var parameters = formatResult.ParameterValues.ToList();

						foreach (var mapping in formatResult.ParameterIndexToPlaceholderIndexes)
						{
							parameters[mapping.Left] = parameters[mapping.Left].ChangeValue(null);
						}

						formatResultForCache = formatResult.ChangeParameterValues(parameters);
					}

					skipFormatResultSubstitution = true;
					cacheInfo = new ProjectorExpressionCacheInfo(projectionExpression, formatResultForCache);
						
					if (this.SqlDatabaseContext.projectionExpressionCache.Count >= ProjectorCacheMaxLimit)
					{
						ProjectionExpressionCacheLogger.Info(() => $"ProjectionExpressionCache has been flushed because it overflowed with a size of {ProjectionExpressionCacheMaxLimit}\n\nProjectionExpression: {projectionExpression}\n\nAt: {new StackTrace()}");
						
						var newCache = new Dictionary<ExpressionCacheKey, ProjectorExpressionCacheInfo>(ProjectorCacheMaxLimit, ExpressionCacheKeyEqualityComparer.Default);

						foreach (var value in oldCache.Take(oldCache.Count / 3))
						{
							newCache[value.Key] = value.Value;
						}

						newCache[key] = cacheInfo;

						this.SqlDatabaseContext.projectionExpressionCache = newCache;
					}
					else
					{
						var newCache = new Dictionary<ExpressionCacheKey, ProjectorExpressionCacheInfo>(oldCache, ExpressionCacheKeyEqualityComparer.Default) { [key] = cacheInfo };

						this.SqlDatabaseContext.projectionExpressionCache = newCache;
					}
				}
				else
				{
					formatResult = cacheInfo.formatResult;
					projectionExpression = cacheInfo.projectionExpression;
					ProjectionExpressionCacheLogger.Debug($"ProjectionExpressionCache hit for expression: {expression}");
				}
			}

			if (placeholderValues == null)
			{
				expression = this.Bind(expression);
				placeholderValues = SqlConstantPlaceholderValuesCollector.CollectValues(expression);
			}
			
			var columns = projectionExpression.Select.Columns.Select(c => c.Name);
			var projector = ProjectionBuilder.Build(this.DataAccessModel, this.SqlDatabaseContext, this, projectionExpression.Projector, new ProjectionBuilderScope(columns.ToArray()));

			return this.BuildExecution(projectionExpression, projector, placeholderValues, formatResult, skipFormatResultSubstitution);
		}

	    public ExecutionBuildResult BuildExecution(SqlProjectionExpression projectionExpression, LambdaExpression projector, object[] placeholderValues, SqlQueryFormatResult formatResult = null, bool skipFormatResultSubstitution = false)
        {
			ProjectorCacheInfo cacheInfo;

		    if (formatResult?.ParameterIndexToPlaceholderIndexes == null)
		    {
			    var projectorForFormat = SqlConstantPlaceholderReplacer.Replace(projectionExpression, placeholderValues);

			    formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectorForFormat);
		    }
		    else if (!skipFormatResultSubstitution)
		    {
			    var parameters = formatResult.ParameterValues.ToList();

			    foreach (var mapping in formatResult.ParameterIndexToPlaceholderIndexes)
			    {
				    parameters[mapping.Left] = parameters[mapping.Left].ChangeValue(placeholderValues[mapping.Right]);
			    }

			    formatResult = formatResult.ChangeParameterValues(parameters);
		    }

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
				if ((projectionExpression.Aggregator?.Body as MethodCallExpression)?.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.EnumerableExtensionsAlwaysReadFirstMethod)
				{
					var constructor = typeof(AlwaysReadFirstObjectProjector<,>).MakeGenericType(projectionLambda.ReturnType, projectionLambda.ReturnType).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

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
			}

	        var asyncExecutor = executor;
			var cancellationToken = Expression.Parameter(typeof(CancellationToken));

			if (projectionExpression.Aggregator != null)
			{
				var originalExecutor = executor;
				var aggr = projectionExpression.Aggregator;
				var newBody = SqlConstantPlaceholderReplacer.Replace(aggr.Body, placeholderValuesParam);
                executor = SqlExpressionReplacer.Replace(newBody, aggr.Parameters[0], originalExecutor);

		        newBody = ProjectionAsyncRewriter.Rewrite(newBody, cancellationToken);
				asyncExecutor = SqlExpressionReplacer.Replace(newBody, aggr.Parameters[0], originalExecutor);
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
				ProjectionCacheLogger.Error(() => $"ProjectorCache has been flushed because it overflowed with a size of {ProjectorCacheMaxLimit}\n\nCommand: {formatResult.CommandText}\n\nProjector: {projector}\n\nAt: {new StackTrace()}");

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

			ProjectionCacheLogger.Info(() => $"Cached projection for query:\n{formatResult.CommandText}\n\nprojector:\n{projector}");
			ProjectionCacheLogger.Debug(() => $"Projector Cache Size: {newCache.Count}");

			return new ExecutionBuildResult(formatResult, cacheInfo.projector, cacheInfo.asyncProjector, placeholderValues);
		}
	}
}
;