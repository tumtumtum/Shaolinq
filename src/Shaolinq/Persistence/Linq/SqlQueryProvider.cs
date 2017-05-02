// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryProvider
		: ReusableQueryProvider
	{
		private readonly string paramPrefix;
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

				createQueryCache = createQueryCache.Clone(elementType.TypeHandle, func, "createQueryCache");
			}

			return func(provider, expression);
		}
		
		public override string GetQueryText(Expression expression)
		{
			expression = (SqlProjectionExpression)Bind(this.DataAccessModel, this.SqlDatabaseContext.SqlDataTypeProvider, expression);

			var projectionExpression = Optimize(this.DataAccessModel, expression);
			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionExpression);

			return this.GetQueryText(formatResult);
		}

		internal string GetParamName(int index) => SqlQueryFormatter.ParamNamePrefix + index;

		internal string GetQueryText(SqlQueryFormatResult formatResult, Func<int, string> toString = null)
		{
			var formatManager = this.SqlDatabaseContext.SqlQueryFormatterManager;

			var sql = formatManager.SubstitutedParameterValues(formatResult.CommandText, (parameterName, formatConstant) =>
			{
				var index = 0;
				var start = parameterName.IndexOf(char.IsDigit);
				
				for (var i = start; i < parameterName.Length; i++)
				{
					index *= 10;
					index = parameterName[i] - '0';
				}
				
				return toString?.Invoke(index) ?? formatConstant(formatResult.ParameterValues[index].Value);
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
			this.paramPrefix = this.SqlDatabaseContext.SqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
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
			return this.BuildExecution(expression).EvaluateAsync<T>(cancellationToken);
		}

		public override IEnumerable<T> GetEnumerable<T>(Expression expression)
		{
			return this.BuildExecution(expression).Evaluate<IEnumerable<T>>();
		}

		public override IAsyncEnumerable<T> GetAsyncEnumerable<T>(Expression expression)
		{
			return this.BuildExecution(expression).EvaluateAsyncEnumerable<T>(CancellationToken.None);
		}

		public static Expression Optimize(DataAccessModel dataAccessModel, Expression expression)
		{
			expression = SqlGroupByCollator.Collate(expression);
			expression = SqlAggregateSubqueryRewriter.Rewrite(expression);
			expression = SqlUnusedColumnRemover.Remove(expression);
			expression = SqlRedundantColumnRemover.Remove(expression);
			expression = SqlRedundantSubqueryRemover.Remove(expression);
			expression = SqlFunctionCoalescer.Coalesce(expression);
			expression = SqlExistsSubqueryOptimizer.Optimize(expression);
			expression = SqlRedundantBinaryExpressionsRemover.Remove(expression);
			expression = SqlCrossJoinRewriter.Rewrite(expression);
			expression = SqlConditionalEliminator.Eliminate(expression);
			expression = SqlExpressionCollectionOperationsExpander.Expand(expression);
			expression = SqlSubCollectionOrderByAmender.Amend(dataAccessModel.TypeDescriptorProvider, expression);
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
			expression = SqlUpdateNormalizer.Normalize(expression);
			expression = SqlInsertIntoNormalizer.Normalize(expression);

			return expression;
		}

		internal static Expression Bind(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, Expression expression)
		{
			var placeholderCount = -1;

			expression = Evaluator.PartialEval(expression, ref placeholderCount);
			expression = QueryBinder.Bind(dataAccessModel, expression);
			expression = SqlEnumTypeNormalizer.Normalize(expression, sqlDataTypeProvider.GetTypeForEnums());
			expression = Evaluator.PartialEval(expression, ref placeholderCount);
			expression = SqlNullComparisonCoalescer.Coalesce(expression);
			expression = SqlTupleOrAnonymousTypeComparisonExpander.Expand(expression);
			expression = SqlObjectOperandComparisonExpander.Expand(expression);
			expression = SqlRedundantFunctionCallRemover.Remove(expression);

			return expression;
		}

		internal ExecutionBuildResult BuildExecution(Expression expression, LambdaExpression projection = null, object[] placeholderValues = null, Expression<Func<IDataReader, object[]>> rootKeys = null)
		{
			ProjectorExpressionCacheInfo cacheInfo;
			var skipFormatResultSubstitution = false;
			var projectionExpression = expression as SqlProjectionExpression ?? (SqlProjectionExpression)Bind(this.DataAccessModel, this.SqlDatabaseContext.SqlDataTypeProvider, expression);

			var foundCachedProjection = false;
			var key = new ExpressionCacheKey(projectionExpression, projection);

			if (!this.SqlDatabaseContext.projectionExpressionCache.TryGetValue(key, out cacheInfo))
			{
				if (expression != projectionExpression)
				{
					placeholderValues = SqlConstantPlaceholderValuesCollector.CollectValues(projectionExpression);
					projectionExpression = (SqlProjectionExpression)Optimize(this.DataAccessModel, projectionExpression);
					skipFormatResultSubstitution = true;
				}

				var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionExpression);

				SqlQueryFormatResult formatResultForCache = null;

				if (formatResult.Cacheable)
				{
					var parameters = formatResult.ParameterValues.ToList();

					foreach (var index in formatResult.ParameterIndexToPlaceholderIndexes.Keys)
					{
						var value = parameters[index];

						parameters[index] = value.ChangeValue(value.Type.GetDefaultValue());
					}

					formatResultForCache = formatResult.ChangeParameterValues(parameters);
				}
				else
				{
					if (!skipFormatResultSubstitution)
					{
						// Edge case where inner projection from ProjectionBuilder can't be cached (related DeflatedPredicated with a complex predicate)

						skipFormatResultSubstitution = true;

						projectionExpression = (SqlProjectionExpression)SqlConstantPlaceholderReplacer.Replace(projectionExpression, placeholderValues);

						formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionExpression);
					}
				}
				
				cacheInfo = new ProjectorExpressionCacheInfo(projectionExpression, formatResultForCache);

				var columns = projectionExpression.Select.Columns.Select(c => c.Name).ToArray();

				if (projection == null)
				{
					projection = ProjectionBuilder.Build(this.DataAccessModel, this.SqlDatabaseContext, this, projectionExpression.Projector, new ProjectionBuilderScope(columns), out rootKeys);
				}

				this.BuildProjector(projection, projectionExpression.Aggregator, rootKeys, out cacheInfo.projector, out cacheInfo.asyncProjector);
				
				this.SqlDatabaseContext.projectionExpressionCache = this.SqlDatabaseContext.projectionExpressionCache.Clone(key, cacheInfo, "ProjectionExpression", this.ProjectionExpressionCacheMaxLimit, ProjectionCacheLogger, c => c.projectionExpression.ToString());

				ProjectionCacheLogger.Debug(() => $"Cached projection for query:\n{this.GetQueryText(formatResult, this.GetParamName)}\n\nProjector:\n{cacheInfo.projector}");
				ProjectionCacheLogger.Debug(() => $"Projector Cache Size: {this.SqlDatabaseContext.projectionExpressionCache.Count}");

				cacheInfo.formatResult = formatResult;
			}
			else
			{
				foundCachedProjection = true;
			}

			if (placeholderValues == null)
			{
				placeholderValues = SqlConstantPlaceholderValuesCollector.CollectValues(projectionExpression);
			}

			if (cacheInfo.formatResult == null)
			{
				var projector = SqlConstantPlaceholderReplacer.Replace(cacheInfo.projectionExpression, placeholderValues);
				var optimizedProjector = Optimize(this.DataAccessModel, projector);

				cacheInfo.formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(optimizedProjector);
			}
			else if (!skipFormatResultSubstitution)
			{
				var parameters = cacheInfo.formatResult.ParameterValues.ToList();
				
				foreach (var indexes in cacheInfo.formatResult.ParameterIndexToPlaceholderIndexes)
				{
					var index = indexes.Key;
					var placeholderIndex = indexes.Value;
					
					parameters[index] = parameters[index].ChangeValue(placeholderValues[placeholderIndex]);
				}

				cacheInfo.formatResult = cacheInfo.formatResult.ChangeParameterValues(parameters);
			}

			if (foundCachedProjection)
			{
				ProjectionCacheLogger.Debug(() => $"Cache hit for query:\n{this.GetQueryText(cacheInfo.formatResult, this.GetParamName)}");
			}

			return new ExecutionBuildResult(this, cacheInfo.formatResult, cacheInfo.projector, cacheInfo.asyncProjector, placeholderValues);
		}

		private void BuildProjector(LambdaExpression projectionLambda, LambdaExpression aggregator, Expression<Func<IDataReader, object[]>> keyBuilder, out Delegate projector, out Delegate asyncProjector)
		{
			var sqlQueryProviderParam = Expression.Parameter(typeof(SqlQueryProvider));
			var formatResultsParam = Expression.Parameter(typeof(SqlQueryFormatResult));
			var placeholderValuesParam = Expression.Parameter(typeof(object[]));

			var elementType = projectionLambda.ReturnType;

			Expression executor;

			if (elementType.IsDataAccessObjectType())
			{
				var concreteElementType = this.DataAccessModel.GetConcreteTypeFromDefinitionType(elementType);

				var constructor = typeof(DataAccessObjectProjector<,>)
					.MakeGenericType(elementType, concreteElementType)
					.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

				executor = Expression.New
				(
					constructor,
					sqlQueryProviderParam,
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
						sqlQueryProviderParam,
						Expression.Constant(this.DataAccessModel),
						Expression.Constant(this.SqlDatabaseContext),
						formatResultsParam,
						placeholderValuesParam,
						projectionLambda
					);
				}
				else
				{
					if (keyBuilder == null)
					{
						var constructor = typeof(ObjectProjector<,>)
							.MakeGenericType(projectionLambda.ReturnType, projectionLambda.ReturnType)
							.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
							.Single();

						executor = Expression.New
						(
							constructor,
							sqlQueryProviderParam,
							Expression.Constant(this.DataAccessModel),
							Expression.Constant(this.SqlDatabaseContext),
							formatResultsParam,
							placeholderValuesParam,
							projectionLambda
						);
					}
					else
					{
						var constructor = typeof(ComplexDataAccessObjectProjector<,>)
							.MakeGenericType(projectionLambda.ReturnType, projectionLambda.ReturnType)
							.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
							.Single();

						executor = Expression.New
						(
							constructor,
							sqlQueryProviderParam,
							Expression.Constant(this.DataAccessModel),
							Expression.Constant(this.SqlDatabaseContext),
							formatResultsParam,
							placeholderValuesParam,
							projectionLambda,
							keyBuilder
						);
					}
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

			projectionLambda = Expression.Lambda(executor, sqlQueryProviderParam, formatResultsParam, placeholderValuesParam);
			var asyncProjectorLambda = Expression.Lambda(asyncExecutor, sqlQueryProviderParam, formatResultsParam, placeholderValuesParam, cancellationToken);

			ProjectorCacheInfo cacheInfo;
			var key = new ProjectorCacheKey(projectionLambda);
			var oldCache = this.SqlDatabaseContext.projectorCache;

			if (!oldCache.TryGetValue(key, out cacheInfo))
			{
				cacheInfo.projector = projectionLambda.Compile();
				cacheInfo.asyncProjector = asyncProjectorLambda.Compile();

				this.SqlDatabaseContext.projectorCache = oldCache.Clone(key, cacheInfo, "projectorCache", this.ProjectorCacheMaxLimit);

				ProjectionCacheLogger.Info(() => $"Cached projector:\n{projectionLambda}");
				ProjectionCacheLogger.Debug(() => $"Projector Cache Size: {this.SqlDatabaseContext.projectionExpressionCache.Count}");
			}
			
			projector = cacheInfo.projector;
			asyncProjector = cacheInfo.asyncProjector;
		}
	}
}