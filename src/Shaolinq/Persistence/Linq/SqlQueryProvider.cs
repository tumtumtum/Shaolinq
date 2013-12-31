// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

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
			var v = this.PrivateExecute(expression);

			switch (v.Second)
			{
				case SelectFirstType.FirstOrDefault:
					return ((IEnumerable<T>)v.First).FirstOrDefault();
				case SelectFirstType.Single:
					return ((IEnumerable<T>)v.First).Single();
				case SelectFirstType.SingleOrDefault:
					return ((IEnumerable<T>)v.First).SingleOrDefault();
				case SelectFirstType.DefaultIfEmpty:
					var retval = ((IEnumerable<T>)v.First).SingleOrDefault();

					if (retval == null || retval.Equals(typeof(T).GetDefaultValue()))
					{
						return (T)Expression.Lambda(v.Third).Compile().DynamicInvoke(null);
					}

					return retval;
				default:
					return ((IEnumerable<T>)v.First).First();
			}
		}

		public override object Execute(Expression expression)
		{
			return PrivateExecute(expression).First;
		}

		public static Expression Optimize(DataAccessModel dataAccessModel, Expression expression)
		{
			expression = GroupByCollator.Collate(expression);
			expression = AggregateRewriter.Rewrite(expression);
			expression = UnusedColumnRemover.Remove(expression);
			expression = RedundantColumnRemover.Remove(expression);
			expression = RedundantSubqueryRemover.Remove(expression);
			expression = FunctionCoalescer.Coalesce(expression);
			//expression = OrderByRewriter.Rewrite(expression);
			expression = RedundantBinaryExpressionsRemover.Remove(expression);
			expression = ObjectOperandComparisonExpander.Expand(expression);
			expression = Evaluator.PartialEval(dataAccessModel, expression);
			expression = RedundantFunctionCallRemover.Remove(expression);
			expression = ConditionalEliminator.Eliminate(expression);
			expression = SqlExpressionCollectionOperationsExpander.Expand(expression);

			return expression;
		}

		private struct ProjectorCacheInfo
		{	
			public Type elementType;
			public Delegate projector;
		}


		private class ProjectorCacheEqualityComparer
			: IEqualityComparer<ProjectorCacheKey>
		{
			public bool Equals(ProjectorCacheKey x, ProjectorCacheKey y)
			{
				return SqlExpressionComparer.Equals(x.projectionExpression, y.projectionExpression, true)
				       && x.sqlDatabaseContext == y.sqlDatabaseContext;
			}

			public int GetHashCode(ProjectorCacheKey obj)
			{
				return obj.hashCode;
			}
		}

		private struct ProjectorCacheKey
		{
			internal readonly int hashCode;
			internal readonly SqlDatabaseContext sqlDatabaseContext;
			internal readonly Expression projectionExpression;

			public ProjectorCacheKey(Expression projectionExpression, SqlDatabaseContext sqlDatabaseContext)
			{
				this.sqlDatabaseContext = sqlDatabaseContext;
				this.projectionExpression = projectionExpression;
				this.hashCode = SqlExpressionHasher.Hash(this.projectionExpression) & sqlDatabaseContext.GetHashCode();
			}
		}

		private static readonly Dictionary<ProjectorCacheKey, ProjectorCacheInfo> ProjectorCache = new Dictionary<ProjectorCacheKey, ProjectorCacheInfo>(new ProjectorCacheEqualityComparer());

		private Triple<object, SelectFirstType, Expression> PrivateExecute(Expression expression)
		{
			var placeholderValues = new object[0];
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

				projectionExpression = (SqlProjectionExpression)Optimize(this.DataAccessModel, expression);
			}

			ProjectorCacheInfo cacheInfo;
			
			var columns = projectionExpression.Select.Columns.Select(c => c.Name);

			var sqlQueryFormatter = this.SqlDatabaseContext.NewQueryFormatter(this.DataAccessModel, this.SqlDatabaseContext.SqlDataTypeProvider, this.SqlDatabaseContext.SqlDialect, projectionExpression, SqlQueryFormatterOptions.Default);
			var formatResult = sqlQueryFormatter.Format();

			placeholderValues = PlaceholderValuesCollector.CollectValues(expression);

			var key = new ProjectorCacheKey(projectionExpression, this.SqlDatabaseContext);

			lock (ProjectorCache)
			{
				if (!ProjectorCache.TryGetValue(key, out cacheInfo))
				{
					const int maxCacheSize = 1024;

					if (ProjectorCache.Count > maxCacheSize)
					{
						Logger.WarnFormat("ProjectorCache/LambdaCache has more than {0} items.  Flushing.", maxCacheSize);
						Logger.WarnFormat("Query Causing Flush: {0}", formatResult);

						ProjectorCache.Clear();
					}

					var projectionLambda = ProjectionBuilder.Build(this.DataAccessModel, this.SqlDatabaseContext, projectionExpression.Projector, columns);

					cacheInfo.elementType = projectionLambda.Body.Type;
					cacheInfo.projector = projectionLambda.Compile();

					ProjectorCache[key] = cacheInfo;
				}
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

				return new Triple<object, SelectFirstType, Expression>
				(
					Activator.CreateInstance
					(
						type.MakeGenericType(elementType, concreteElementType),
						this,
						this.DataAccessModel,
						formatResult,
						this.SqlDatabaseContext,
						cacheInfo.projector,
						this.RelatedDataAccessObjectContext,
						projectionExpression.SelectFirstType,
						placeholderValues
					),
					projectionExpression.SelectFirstType,
					projectionExpression.DefaultValueExpression
				);
			}
			else
			{
				return new Triple<object, SelectFirstType, Expression>
				(
					Activator.CreateInstance
					(
						typeof(ObjectProjector<,>).MakeGenericType(elementType, concreteElementType),
						this,
						this.DataAccessModel,
						formatResult,
						this.SqlDatabaseContext,
						cacheInfo.projector,
						this.RelatedDataAccessObjectContext,
						projectionExpression.SelectFirstType,
						placeholderValues
					),
					projectionExpression.SelectFirstType,
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

			var sqlQueryFormatter = this.SqlDatabaseContext.NewQueryFormatter(this.DataAccessModel, this.SqlDatabaseContext.SqlDataTypeProvider, this.SqlDatabaseContext.SqlDialect, projectionExpression, SqlQueryFormatterOptions.Default);
			
			return sqlQueryFormatter.Format().CommandText;
		}
	}
}
