// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
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
		private readonly ProjectionScope projectionScope;
		protected static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

		protected internal struct ProjectorCacheKey
		{
			internal readonly int hashCode;
			internal readonly Expression projectionExpression;
			internal readonly Expression aggregator;

			public ProjectorCacheKey(Expression projectionExpression, Expression aggregator)
			{
				this.projectionExpression = projectionExpression;
				this.aggregator = aggregator;
				this.hashCode = SqlExpressionHasher.Hash(this.projectionExpression) 
					^ (aggregator == null ? 0 : SqlExpressionHasher.Hash(aggregator));
			}
		}

		protected internal struct ProjectorCacheInfo
		{
			public Type elementType;
			public Delegate projector;
		}

		protected internal class ProjectorCacheEqualityComparer
			: IEqualityComparer<ProjectorCacheKey>
		{
			public static ProjectorCacheEqualityComparer Default = new ProjectorCacheEqualityComparer();

			public bool Equals(ProjectorCacheKey x, ProjectorCacheKey y)
			{
				return SqlExpressionComparer.Equals(x.projectionExpression, y.projectionExpression, SqlExpressionComparerOptions.IgnoreConstantPlaceholders)
					&& (SqlExpressionComparer.Equals(x.aggregator, y.aggregator, SqlExpressionComparerOptions.IgnoreConstantPlaceholders));
			}

			public int GetHashCode(ProjectorCacheKey obj)
			{
				return obj.hashCode;
			}
		}

		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }

		public SqlQueryProvider(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, ProjectionScope projectionScope)
			: base(typeof(SqlQueryable<>))
		{
			this.projectionScope = projectionScope;
			this.DataAccessModel = dataAccessModel;
			this.SqlDatabaseContext = sqlDatabaseContext;
		}
		
		public override IQueryable<T> CreateQuery<T>(Expression expression)
		{
			return new SqlQueryable<T>(this, expression);
		}

		public override T Execute<T>(Expression expression)
		{
			return this.PrivateExecute<T>(expression);
		}

		public override object Execute(Expression expression)
		{
			return this.Execute<object>(expression);
		}

		public override IEnumerable<T> GetEnumerable<T>(Expression expression)
		{
			return this.PrivateExecute<IEnumerable<T>>(expression);
		}

		public static Expression Optimize(Expression expression, Type typeForEnums, bool simplerPartialVal = true)
		{
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
			
			if (simplerPartialVal)
			{
				expression = Evaluator.PartialEval(expression, c => c.NodeType != (ExpressionType)SqlExpressionType.ConstantPlaceholder && Evaluator.CanBeEvaluatedLocally(c));
			}
			else
			{
				expression = Evaluator.PartialEval(expression);
			}

			expression = SqlRedundantFunctionCallRemover.Remove(expression);
			expression = SqlConditionalEliminator.Eliminate(expression);
			expression = SqlExpressionCollectionOperationsExpander.Expand(expression);
			expression = SqlSumAggregatesDefaultValueCoalescer.Coalesce(expression);
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
		
		private T PrivateExecute<T>(Expression expression)
		{
			var projectionExpression = expression as SqlProjectionExpression;

			if (projectionExpression == null)
			{
				expression = Evaluator.PartialEval(expression);

				if (this.RelatedDataAccessObjectContext == null)
				{
					expression = QueryBinder.Bind(this.DataAccessModel, expression, null, null);
				}
				else
				{
					expression = QueryBinder.Bind(this.DataAccessModel, expression, this.RelatedDataAccessObjectContext.ElementType, this.RelatedDataAccessObjectContext.ExtraCondition);
				}

				expression = projectionExpression = (SqlProjectionExpression)Optimize(expression, this.SqlDatabaseContext.SqlDataTypeProvider.GetTypeForEnums(), true);
			}

			ProjectorCacheInfo cacheInfo;
			var key = new ProjectorCacheKey(expression, projectionExpression.Aggregator);
			var projectorCache = this.SqlDatabaseContext.projectorCache;
			
			if (!projectorCache.TryGetValue(key, out cacheInfo))
			{
				var projectorInfoParam = Expression.Parameter(typeof(ProjectorInfo));
				var columns = projectionExpression.Select.Columns.Select(c => c.Name);
				var projectionLambda = ProjectionBuilder.Build(this.DataAccessModel, this.SqlDatabaseContext, projectionScope, projectionExpression.Projector, columns);

				var elementType = projectionLambda.ReturnType;
                
				if (elementType.IsDataAccessObjectType())
				{
					var concreteElementType = this.DataAccessModel.GetConcreteTypeFromDefinitionType(elementType);

					if (this.RelatedDataAccessObjectContext == null)
					{
						var constructor = typeof(DataAccessObjectProjector<,>).MakeGenericType(elementType, concreteElementType).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

						expression = Expression.New
						(
							constructor,
							Expression.Constant(this),
							Expression.Constant(this.DataAccessModel),
							Expression.Constant(this.SqlDatabaseContext),
							Expression.Constant(this.RelatedDataAccessObjectContext, typeof(IRelatedDataAccessObjectContext)),
							projectorInfoParam,
							projectionLambda
						);
					}
					else
					{
						var constructor = typeof(RelatedDataAccessObjectProjector<,>).MakeGenericType(elementType, concreteElementType).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

						expression = Expression.New
						(
							constructor,
							Expression.Constant(this),
							Expression.Constant(this.DataAccessModel),
							Expression.Constant(this.SqlDatabaseContext),
							Expression.Constant(this.RelatedDataAccessObjectContext, typeof(IRelatedDataAccessObjectContext)),
							projectorInfoParam,
							projectionLambda
						);
					}
				}
				else
				{
					var constructor = typeof(ObjectProjector<,>).MakeGenericType(projectionLambda.ReturnType, projectionLambda.ReturnType).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

					expression = Expression.New
					(
						constructor,
						Expression.Constant(this),
						Expression.Constant(this.DataAccessModel),
						Expression.Constant(this.SqlDatabaseContext),
						Expression.Constant(this.RelatedDataAccessObjectContext, typeof(IRelatedDataAccessObjectContext)),
						projectorInfoParam,
						projectionLambda
					);
				}

				if (projectionExpression.Aggregator != null)
				{
					expression = SqlExpressionReplacer.Replace(projectionExpression.Aggregator.Body, projectionExpression.Aggregator.Parameters[0], expression);
				}

				cacheInfo.elementType = projectionLambda.Body.Type;
				cacheInfo.projector = (Func<ProjectorInfo, T>)Expression.Lambda(expression, projectorInfoParam).Compile();

				var newCache = new Dictionary<ProjectorCacheKey, ProjectorCacheInfo>(projectorCache, ProjectorCacheEqualityComparer.Default);

				if (!projectorCache.ContainsKey(key))
				{
					newCache[key] = cacheInfo;
				}

				this.SqlDatabaseContext.projectorCache = newCache;
			}

			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionExpression, SqlQueryFormatterOptions.Default);
			var placeholderValues = PlaceholderValuesCollector.CollectValues(expression);

			var projectorInfo = new ProjectorInfo
			{
				FormatResult = formatResult,
				PlaceholderValues = placeholderValues
			};

			return (T)((Func<ProjectorInfo, T>)cacheInfo.projector)(projectorInfo);
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
	}
}
