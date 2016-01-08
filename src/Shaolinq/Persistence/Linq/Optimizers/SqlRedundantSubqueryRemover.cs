// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Removes select expressions that don't add any additional semantic value
	/// </summary>
	public class SqlRedundantSubqueryRemover
		: SqlExpressionVisitor
	{
		private SqlRedundantSubqueryRemover()
		{
		}

		public static Expression Remove(Expression expression)
		{
			expression = new SqlRedundantSubqueryRemover().Visit(expression);
			expression = SubqueryMerger.Merge(expression);
			//expression = AggregateSubqueryMerger.Merge(expression);

			return expression;
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			select = (SqlSelectExpression)base.VisitSelect(select);

			// Expand all purely redundant subqueries

			var redundantQueries = RedundantSubqueryFinder.Find(select.From);

			if (redundantQueries != null)
			{
				select = SubqueryRemover.Remove(select, redundantQueries);
			}

			return select;
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			projection = (SqlProjectionExpression)base.VisitProjection(projection);

			if (projection.Select.From is SqlSelectExpression)
			{
				var redundantQueries = RedundantSubqueryFinder.Find(projection.Select);

				if (redundantQueries != null)
				{
					projection = SubqueryRemover.Remove(projection, redundantQueries);
				}
			}

			return projection;
		}

		private class SubqueryMerger
			: SqlExpressionVisitor
		{
			private SubqueryMerger()
			{
			}

			public static Expression Merge(Expression expression)
			{
				return new SubqueryMerger().Visit(expression);
			}

			protected override Expression VisitSelect(SqlSelectExpression select)
			{
				select = (SqlSelectExpression)base.VisitSelect(select);

				// Attempt to merge subqueries that would have been removed by the above
				// logic except for the existence of a where clause

				while (CanMergeWithFrom(select))
				{
					var fromSelect = GetLeftMostSelect(select.From);

					// remove the redundant subquery
					select = SubqueryRemover.Remove(select, fromSelect);

					// merge where expressions 
					var where = select.Where;

					if (fromSelect.Where != null)
					{
						if (where != null)
						{
							where = Expression.And(fromSelect.Where, where);
						}
						else
						{
							where = fromSelect.Where;
						}
					}

					var orderBy = select.OrderBy != null && select.OrderBy.Count > 0 ? select.OrderBy : fromSelect.OrderBy;
					var groupBy = select.GroupBy != null && select.GroupBy.Count > 0 ? select.GroupBy : fromSelect.GroupBy;
					var skip = select.Skip ?? fromSelect.Skip;
					var take = select.Take ?? fromSelect.Take;
					var isDistinct = select.Distinct | fromSelect.Distinct;

					if (where != select.Where || orderBy != select.OrderBy || groupBy != select.GroupBy || isDistinct != select.Distinct || skip != select.Skip || take != select.Take)
					{
						select = new SqlSelectExpression(select.Type, select.Alias, select.Columns, select.From, where, orderBy, groupBy, isDistinct, skip, take, select.ForUpdate);
					}
				}

				return select;
			}

			private static SqlSelectExpression GetLeftMostSelect(Expression source)
			{
				var select = source as SqlSelectExpression;

				if (select != null)
				{
					return select;
				}

				var join = source as SqlJoinExpression;

				if (join != null)
				{
					return GetLeftMostSelect(join.Left);
				}

				return null;
			}

			private static bool IsColumnProjection(SqlSelectExpression select)
			{
				var count = select.Columns.Count;

				for (var i = 0; i < count; i++)
				{
					var columnDeclaration = select.Columns[i];

					if (columnDeclaration.Expression.NodeType != (ExpressionType)SqlExpressionType.Column && columnDeclaration.Expression.NodeType != ExpressionType.Constant)
					{
						return false;
					}
				}

				return true;
			}

			private static bool CanMergeWithFrom(SqlSelectExpression select)
			{
				var fromSelect = GetLeftMostSelect(select.From);

				if (fromSelect == null)
				{
					return false;
				}

				if (fromSelect.Columns.Any(c => c.NoOptimise))
				{
					return false;
				}

				if (!IsColumnProjection(fromSelect))
				{
					return false;
				}

				var selHasNameMapProjection = RedundantSubqueryFinder.IsNameMapProjection(select);
				var selHasSkip = select.Skip != null; 
				var selHasWhere = select.Where != null;
				var selHasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
				var selHasGroupBy = select.GroupBy != null && select.GroupBy.Count > 0;
				var selHasAggregates = SqlAggregateChecker.HasAggregates(select);
				var frmHasOrderBy = fromSelect.OrderBy != null && fromSelect.OrderBy.Count > 0;
				var frmHasGroupBy = fromSelect.GroupBy != null && fromSelect.GroupBy.Count > 0;

				// Both cannot have OrderBy
				if (selHasOrderBy && frmHasOrderBy)
				{
					return false;
				}

				// Both cannot have GroupBy
				if (selHasGroupBy && frmHasGroupBy)
				{
					return false;
				}

				// Cannot move forward OrderBy if outer has GroupBy
				if (frmHasOrderBy && (selHasGroupBy || selHasAggregates || select.Distinct))
				{
					return false;
				}

				// Cannot move forward GroupBy if outer has a where clause
				if (frmHasGroupBy && (select.Where != null))
				{
					return false;
				}

				// Cannot move forward a take if outer has take or skip or distinct
				if (fromSelect.Take != null && (select.Take != null || selHasSkip  || select.Distinct || selHasAggregates || selHasGroupBy))
				{
					return false;
				}

				// Cannot move forward a skip if outer has skip or distinct or aggregates with accompanying groupby or where
				if (fromSelect.Skip != null && ((selHasWhere || selHasGroupBy) && (select.Skip != null || select.Distinct || selHasAggregates)))
				{
					return false;
				}

				// Cannot merge a distinct if the outer has a distinct or aggregates with accompanying groupby or where
				if (fromSelect.Distinct && (!selHasNameMapProjection || (((selHasWhere || selHasGroupBy) && (selHasAggregates || selHasOrderBy)))))
				{
					return false;
				}

				return true;
			}
		}
	}
}
