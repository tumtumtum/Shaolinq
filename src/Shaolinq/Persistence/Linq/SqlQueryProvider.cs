// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using log4net;
using Platform;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryProvider
		: ReusableQueryProvider
	{
		internal protected struct PrivateExecuteResult<T>
		{	
			public IEnumerable<T> results;
			private bool computedDefaultValue;
			public SqlAggregateType sqlAggregateType;
			private T defaultValue;
			public SelectFirstType selectFirstType;
			public bool defaultIfEmpty;
			public Expression defaultValueExpression;

			public PrivateExecuteResult(IEnumerable<T> results, SelectFirstType selectFirstType, SqlAggregateType sqlAggregateType, bool defaultIfEmpty, Expression defaultValueExpression)
				: this()
			{
				this.results = results;
				this.sqlAggregateType = sqlAggregateType;
				this.selectFirstType = selectFirstType;
				this.defaultIfEmpty = defaultIfEmpty;
				this.defaultValueExpression = defaultValueExpression;
			}

			public T GetDefaultValue()
			{
				if (!computedDefaultValue)
				{
					if (defaultValueExpression == null)
					{
						defaultValue = default(T);
					}
					else
					{
						defaultValue = (T)Expression.Lambda(this.defaultValueExpression).Compile().DynamicInvoke(null);
					}

					computedDefaultValue = true;
				}

				return defaultValue;
			}
		}

		internal protected struct ProjectorCacheKey
		{
			internal readonly int hashCode;
			internal readonly Expression projectionExpression;

			public ProjectorCacheKey(Expression projectionExpression, SqlDatabaseContext sqlDatabaseContext)
			{
				this.projectionExpression = projectionExpression;
				this.hashCode = SqlExpressionHasher.Hash(this.projectionExpression) & sqlDatabaseContext.GetHashCode();
			}
		}

		internal protected struct ProjectorCacheInfo
		{
			public SqlAggregateType sqlAggregateType;
			public Type elementType;
			public Delegate projector;
		}

		protected internal class ProjectorCacheEqualityComparer
			: IEqualityComparer<ProjectorCacheKey>
		{
			public static ProjectorCacheEqualityComparer Default = new ProjectorCacheEqualityComparer();

			public bool Equals(ProjectorCacheKey x, ProjectorCacheKey y)
			{
				return SqlExpressionComparer.Equals(x.projectionExpression, y.projectionExpression, true);
			}

			public int GetHashCode(ProjectorCacheKey obj)
			{
				return obj.hashCode;
			}
		}

		public static readonly ILog Logger = LogManager.GetLogger(typeof(Sql92QueryFormatter));

		public DataAccessModel DataAccessModel { get; private set; }
		public SqlDatabaseContext SqlDatabaseContext { get; private set; }

		public SqlQueryProvider(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext)
			: base(typeof(SqlQueryable<>))
		{
			this.DataAccessModel = dataAccessModel;
			this.SqlDatabaseContext = sqlDatabaseContext;
		}
		
		public override IQueryable<T> CreateQuery<T>(Expression expression)
		{
			return new SqlQueryable<T>(this, expression);
		}

		public override T Execute<T>(Expression expression)
		{
			IEnumerable<T> results;
			var privateExecuteResult = this.PrivateExecute<T>(expression);

			if (privateExecuteResult.defaultIfEmpty)
			{
				T firstValue;

				if (!privateExecuteResult.results.TryGetFirst(out firstValue))
				{
					results = new List<T>
					{
						privateExecuteResult.GetDefaultValue()
					};
				}
				else
				{
					results = new [] { firstValue }.Concat(privateExecuteResult.results);
				}
			}
			else
			{
				results = privateExecuteResult.results;
			}

			switch (privateExecuteResult.selectFirstType)
			{
				case SelectFirstType.First:
					return results.First();
				case SelectFirstType.FirstOrDefault:
					return results.FirstOrDefault();
				case SelectFirstType.Single:
					return results.Single();
				case SelectFirstType.SingleOrDefault:
					return results.SingleOrDefault();
				default:
					return results.First();
			}
		}

		public override object Execute(Expression expression)
		{
			return Execute<object>(expression);
		}

		public override IEnumerable<T> GetEnumerable<T>(Expression expression)
		{
			var privateExecuteResult = PrivateExecute<T>(expression);

			if (privateExecuteResult.defaultIfEmpty)
			{
				var found = false;

				foreach (var result in privateExecuteResult.results)
				{
					found = true;

					yield return result;
				}

				if (!found)
				{
					yield return privateExecuteResult.GetDefaultValue();
				}
			}
			else
			{
				foreach (var result in privateExecuteResult.results)
				{
					yield return result;
				}
			}
		}

		public static Expression Optimize(DataAccessModel dataAccessModel, Expression expression, SqlDatabaseContext sqlDatabaseContext, bool simplerPartialVal = true)
		{
			expression = EnumTypeNormalizer.Normalize(expression, dataAccessModel, sqlDatabaseContext.SqlDataTypeProvider.GetTypeForEnums());
			expression = GroupByCollator.Collate(expression);
			expression = AggregateSubqueryRewriter.Rewrite(expression);
			expression = UnusedColumnRemover.Remove(expression);
			expression = RedundantColumnRemover.Remove(expression);
			expression = ObjectOperandComparisonExpander.Expand(dataAccessModel, expression);
			expression = RedundantSubqueryRemover.Remove(expression);
			expression = FunctionCoalescer.Coalesce(expression);
			expression = ExistsSubqueryOptimizer.Optimize(expression);
			//expression = OrderByRewriter.Rewrite(expression);
			expression = RedundantBinaryExpressionsRemover.Remove(expression);

			if (simplerPartialVal)
			{
				expression = Evaluator.PartialEval(dataAccessModel, expression, c => c.NodeType != (ExpressionType)SqlExpressionType.ConstantPlaceholder && Evaluator.CanBeEvaluatedLocally(c));
			}
			else
			{
				expression = Evaluator.PartialEval(dataAccessModel, expression);
			}

			expression = RedundantFunctionCallRemover.Remove(expression);
			expression = ConditionalEliminator.Eliminate(expression);
			expression = SqlExpressionCollectionOperationsExpander.Expand(expression);
			expression = SumAggregatesDefaultValueCoalescer.Coalesce(expression);

			return expression;
		}

		private PrivateExecuteResult<T> PrivateExecute<T>(Expression expression)
		{
			var projectionExpression = expression as SqlProjectionExpression;
			
			if (projectionExpression == null)
			{
				expression = Evaluator.PartialEval(this.DataAccessModel, expression);

				if (this.RelatedDataAccessObjectContext == null)
				{
					expression = QueryBinder.Bind(this.DataAccessModel, expression, null, null);
				}
				else
				{
					expression = QueryBinder.Bind(this.DataAccessModel, expression, this.RelatedDataAccessObjectContext.ElementType, this.RelatedDataAccessObjectContext.ExtraCondition);
				}

				projectionExpression = (SqlProjectionExpression)Optimize(this.DataAccessModel, expression, this.SqlDatabaseContext, true);
			}

			ProjectorCacheInfo cacheInfo;
			
			var columns = projectionExpression.Select.Columns.Select(c => c.Name);

			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(projectionExpression, SqlQueryFormatterOptions.Default);
			
			var placeholderValues = PlaceholderValuesCollector.CollectValues(expression);

			var key = new ProjectorCacheKey(projectionExpression, this.SqlDatabaseContext);

			var projectorCache = this.SqlDatabaseContext.projectorCache;

			if (!projectorCache.TryGetValue(key, out cacheInfo))
			{
				var projectionLambda = ProjectionBuilder.Build(this.DataAccessModel, this.SqlDatabaseContext, projectionExpression.Projector, columns);

				cacheInfo.elementType = projectionLambda.Body.Type;
				cacheInfo.projector = projectionLambda.Compile();

				var aggregates = AggregateFinder.Find(projectionExpression);

				if (aggregates.Count == 1)
				{
					cacheInfo.sqlAggregateType = aggregates.First().AggregateType;
				}

				var newCache = new Dictionary<ProjectorCacheKey, ProjectorCacheInfo>(projectorCache, ProjectorCacheEqualityComparer.Default);

				if (!projectorCache.ContainsKey(key))
				{
					newCache[key] = cacheInfo;
				}

				this.SqlDatabaseContext.projectorCache = newCache;
			}
			
			var elementType = TypeHelper.GetElementType(cacheInfo.elementType);
			var concreteElementType = elementType;

			if (elementType.IsDataAccessObjectType())
			{
				Type type;
				elementType = this.DataAccessModel.GetDefinitionTypeFromConcreteType(elementType);
				concreteElementType = this.DataAccessModel.GetConcreteTypeFromDefinitionType(elementType);

				if (this.RelatedDataAccessObjectContext == null)
				{
					type = typeof(DataAccessObjectProjector<,>);
				}
				else
				{
					type = typeof(RelatedDataAccessObjectProjector<,>);
				}

				return new PrivateExecuteResult<T>
				(
					(IEnumerable<T>)Activator.CreateInstance
					(
						type.MakeGenericType(elementType, concreteElementType),
						this,
						this.DataAccessModel,
						formatResult,
						this.SqlDatabaseContext,
						cacheInfo.projector,
						this.RelatedDataAccessObjectContext,
						projectionExpression.SelectFirstType,
						cacheInfo.sqlAggregateType,
						projectionExpression.IsDefaultIfEmpty,
						placeholderValues
					),
					projectionExpression.SelectFirstType,
					cacheInfo.sqlAggregateType,
					projectionExpression.IsDefaultIfEmpty,
					projectionExpression.DefaultValueExpression
				);
			}
			else
			{
				return new PrivateExecuteResult<T>
				(
					(IEnumerable<T>)Activator.CreateInstance
					(
						typeof(ObjectProjector<,>).MakeGenericType(elementType, concreteElementType),
						this,
						this.DataAccessModel,
						formatResult,
						this.SqlDatabaseContext,
						cacheInfo.projector,
						this.RelatedDataAccessObjectContext,
						projectionExpression.SelectFirstType,
						cacheInfo.sqlAggregateType,
						projectionExpression.IsDefaultIfEmpty,
						placeholderValues
					),
					projectionExpression.SelectFirstType,
					cacheInfo.sqlAggregateType,
					projectionExpression.IsDefaultIfEmpty,
					projectionExpression.DefaultValueExpression
				);
			}
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
