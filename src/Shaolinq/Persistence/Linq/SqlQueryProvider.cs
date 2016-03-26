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

		internal ExecutionBuildResult BuildExecution(Expression expression, LambdaExpression projection = null, object[] placeholderValues = null)
		{
			ProjectorExpressionCacheInfo cacheInfo;
			var skipFormatResultSubstitution = false;
			var projectionExpression = expression as SqlProjectionExpression ?? (SqlProjectionExpression)this.Bind(expression);

			var key = new ExpressionCacheKey(projectionExpression, projection);

			if (!this.SqlDatabaseContext.projectionExpressionCache.TryGetValue(key, out cacheInfo))
			{
				if (expression != projectionExpression)
				{
					placeholderValues = SqlConstantPlaceholderValuesCollector.CollectValues(projectionExpression);
					projectionExpression = (SqlProjectionExpression)Optimize(this.DataAccessModel, this.SqlDatabaseContext, projectionExpression);
					skipFormatResultSubstitution = true;
				}

				var oldCache = this.SqlDatabaseContext.projectionExpressionCache;
				var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionExpression);

				var formatResultForCache = formatResult;

				if (formatResult.ParameterIndexToPlaceholderIndexes != null)
				{
					var parameters = formatResult.ParameterValues.ToList();

					foreach (var mapping in formatResult.ParameterIndexToPlaceholderIndexes)
					{
						var value = parameters[mapping.Left];

						parameters[mapping.Left] = value.ChangeValue(value.Type.GetDefaultValue());
					}

					formatResultForCache = formatResult.ChangeParameterValues(parameters);
				}

				
				cacheInfo = new ProjectorExpressionCacheInfo(projectionExpression, formatResultForCache);

				if (projection == null)
				{
					var columns = projectionExpression.Select.Columns.Select(c => c.Name);

					projection = ProjectionBuilder.Build(this.DataAccessModel, this.SqlDatabaseContext, this, projectionExpression.Projector, new ProjectionBuilderScope(columns.ToArray()));
				}

				BuildProjector(projection, projectionExpression.Aggregator, out cacheInfo.projector, out cacheInfo.asyncProjector);

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

				ProjectionCacheLogger.Info(() => $"Cached projection for query:\n{formatResult.CommandText}\n\nprojector:\n{cacheInfo.projector}");
				ProjectionCacheLogger.Debug(() => $"Projector Cache Size: {this.SqlDatabaseContext.projectionExpressionCache.Count}");

				cacheInfo.formatResult = formatResult;
			}
			else
			{
				ProjectionCacheLogger.Info(() => $"Cache hit for query:\n{cacheInfo.formatResult.CommandText}");
			}

			if (placeholderValues == null)
			{
				placeholderValues = SqlConstantPlaceholderValuesCollector.CollectValues(projectionExpression);
			}

			if (cacheInfo.formatResult?.ParameterIndexToPlaceholderIndexes == null)
			{
				var projectorForFormat = SqlConstantPlaceholderReplacer.Replace(cacheInfo.projectionExpression, placeholderValues);

				cacheInfo.formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectorForFormat);
			}
			else if (!skipFormatResultSubstitution)
			{
				var parameters = cacheInfo.formatResult.ParameterValues.ToList();

				foreach (var mapping in cacheInfo.formatResult.ParameterIndexToPlaceholderIndexes)
				{
					parameters[mapping.Left] = parameters[mapping.Left].ChangeValue(placeholderValues[mapping.Right]);
				}

				cacheInfo.formatResult = cacheInfo.formatResult.ChangeParameterValues(parameters);
			}

			return new ExecutionBuildResult(cacheInfo.formatResult, cacheInfo.projector, cacheInfo.asyncProjector, placeholderValues);
		}

		private void BuildProjector(LambdaExpression projectionLambda, LambdaExpression aggregator, out Delegate projector, out Delegate asyncProjector)
		{
			var formatResultsParam = Expression.Parameter(typeof(SqlQueryFormatResult));
			var placeholderValuesParam = Expression.Parameter(typeof(object[]));

			var elementType = projectionLambda.ReturnType;

			Expression executor;

			if (elementType.IsDataAccessObjectType())
			{
				var concreteElementType = this.DataAccessModel.GetConcreteTypeFromDefinitionType(elementType);

				var constructor = typeof(DataAccessObjectProjector<,>).MakeGenericType(elementType, concreteElementType).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

				executor = Expression.New
				(
					constructor,
					Expression.Constant(this),
					Expression.Constant(this.DataAccessModel),
					Expression.Constant(this.SqlDatabaseContext),
					formatResultsParam,
					placeholderValuesParam,
					projectionLambda
				);
			}
			else
			{
				if ((aggregator?.Body as MethodCallExpression)?.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.EnumerableExtensionsAlwaysReadFirstMethod)
				{
					var constructor = typeof(AlwaysReadFirstObjectProjector<,>).MakeGenericType(projectionLambda.ReturnType, projectionLambda.ReturnType).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

					executor = Expression.New
					(
						constructor,
						Expression.Constant(this),
						Expression.Constant(this.DataAccessModel),
						Expression.Constant(this.SqlDatabaseContext),
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
						formatResultsParam,
						placeholderValuesParam,
						projectionLambda
					);
				}
			}

			var asyncExecutor = executor;
			var cancellationToken = Expression.Parameter(typeof(CancellationToken));

			if (aggregator != null)
			{
				var originalExecutor = executor;
				var aggr = aggregator;
				var newBody = SqlConstantPlaceholderReplacer.Replace(aggr.Body, placeholderValuesParam);
				executor = SqlExpressionReplacer.Replace(newBody, aggr.Parameters[0], originalExecutor);

				newBody = ProjectionAsyncRewriter.Rewrite(newBody, cancellationToken);
				asyncExecutor = SqlExpressionReplacer.Replace(newBody, aggr.Parameters[0], originalExecutor);
			}

			projectionLambda = Expression.Lambda(executor, formatResultsParam, placeholderValuesParam);
			var asyncProjectorLambda = Expression.Lambda(asyncExecutor, formatResultsParam, placeholderValuesParam, cancellationToken);

			ProjectorCacheInfo cacheInfo;
			var key = new ProjectorCacheKey(projectionLambda);
			var oldCache = this.SqlDatabaseContext.projectorCache;

			if (!oldCache.TryGetValue(key, out cacheInfo))
			{
				cacheInfo.projector = projectionLambda.Compile();
				cacheInfo.asyncProjector = asyncProjectorLambda.Compile();

				if (this.SqlDatabaseContext.projectorCache.Count >= ProjectorCacheMaxLimit)
				{
					ProjectionExpressionCacheLogger.Info(() => $"Projector has been flushed because it overflowed with a size of {ProjectionExpressionCacheMaxLimit}\n\nProjector: {projectionLambda}\n\nAt: {new StackTrace()}");

					var newCache = new Dictionary<ProjectorCacheKey, ProjectorCacheInfo>(ProjectorCacheMaxLimit, ProjectorCacheEqualityComparer.Default);

					foreach (var value in oldCache.Take(oldCache.Count / 3))
					{
						newCache[value.Key] = value.Value;
					}

					newCache[key] = cacheInfo;

					this.SqlDatabaseContext.projectorCache = newCache;
				}
				else
				{
					var newCache = new Dictionary<ProjectorCacheKey, ProjectorCacheInfo>(oldCache, ProjectorCacheEqualityComparer.Default) { [key] = cacheInfo };

					this.SqlDatabaseContext.projectorCache = newCache;
				}

				ProjectionCacheLogger.Info(() => $"Cached projector:\n{cacheInfo.projector}");
				ProjectionCacheLogger.Debug(() => $"Projector Cache Size: {this.SqlDatabaseContext.projectionExpressionCache.Count}");
			}
			
			projector = cacheInfo.projector;
			asyncProjector = cacheInfo.asyncProjector;
		}
	}
}