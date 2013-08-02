using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	/// <summary>
	/// Removes select expressions that don't add any additional semantic value
	/// </summary>
	public class RedundantSubqueryRemover
		: SqlExpressionVisitor
	{
		private RedundantSubqueryRemover()
		{
		}

		public static Expression Remove(Expression expression)
		{
			expression = new RedundantSubqueryRemover().Visit(expression);
			expression = SubqueryMerger.Merge(expression);
			// expression = AggregateSubqueryMerger.Merge(expression);

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
			bool isTopLevel = true;
			
			private SubqueryMerger()
			{
			}

			public static Expression Merge(Expression expression)
			{
				return new SubqueryMerger().Visit(expression);
			}

			protected override Expression VisitSelect(SqlSelectExpression select)
			{
				bool wasTopLevel = isTopLevel;
				
				isTopLevel = false;

				select = (SqlSelectExpression)base.VisitSelect(select);

				// Attempt to merge subqueries that would have been removed by the above
				// logic except for the existence of a where clause

				while (CanMergeWithFrom(select, wasTopLevel))
				{
					var fromSelect = GetLeftMostSelect(select.From);

					// remove the redundant subquery
					select = SubqueryRemover.Remove(select, fromSelect);

					// merge where expressions 
					Expression where = select.Where;

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
					bool isDistinct = select.Distinct | fromSelect.Distinct;

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
				for (int i = 0, n = select.Columns.Count; i < n; i++)
				{
					var columnDeclaration = select.Columns[i];

					if (columnDeclaration.Expression.NodeType != (ExpressionType)SqlExpressionType.Column && columnDeclaration.Expression.NodeType != ExpressionType.Constant)
					{
						return false;
					}
				}

				return true;
			}

			private static bool CanMergeWithFrom(SqlSelectExpression select, bool isTopLevel)
			{
				var fromSelect = GetLeftMostSelect(select.From);

				if (fromSelect == null)
				{
					return false;
				}

				if (!IsColumnProjection(fromSelect))
				{
					return false;
				}

				var selHasNameMapProjection = RedundantSubqueryFinder.IsNameMapProjection(select);
				var selHasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
				var selHasGroupBy = select.GroupBy != null && select.GroupBy.Count > 0;
				var selHasAggregates = HasAggregateChecker.HasAggregates(select);
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

				// Cannot move forward OuterBy if outer has GroupBy
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
				if (fromSelect.Take != null && (select.Take != null || select.Skip != null || select.Distinct || selHasAggregates || selHasGroupBy))
				{
					return false;
				}

				// Cannot move forward a skip if outer has skip or distinct
				if (fromSelect.Skip != null && (select.Skip != null || select.Distinct || selHasAggregates || selHasGroupBy))
				{
					return false;
				}

				if (fromSelect.Distinct && !selHasNameMapProjection || selHasGroupBy || (selHasAggregates && !isTopLevel)  || (selHasOrderBy && !isTopLevel))
				{
					return false;
				}

				return true;
			}
		}
	}
}