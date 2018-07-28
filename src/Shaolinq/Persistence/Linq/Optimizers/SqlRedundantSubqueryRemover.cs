// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

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

			return expression;
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			select = (SqlSelectExpression)base.VisitSelect(select);

			// Expand all purely redundant subqueries

			var redundantQueries = SqlRedundantSubqueryFinder.Find(select.From);

			if (redundantQueries != null)
			{
				select = SqlSubqueryRemover.Remove(select, redundantQueries);
			}

			return select;
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			projection = (SqlProjectionExpression)base.VisitProjection(projection);

			if (projection.Select.From is SqlSelectExpression)
			{
				var redundantQueries = SqlRedundantSubqueryFinder.Find(projection.Select);

				if (redundantQueries != null)
				{
					projection = SqlSubqueryRemover.Remove(projection, redundantQueries);
				}
			}

			return projection;
		}

		private class SubqueryMerger
			: SqlExpressionVisitor
		{
			private bool isTopLevel;

			private SubqueryMerger()
			{
			}

			public static Expression Merge(Expression expression)
			{
				return new SubqueryMerger().Visit(expression);
			}

			protected override Expression VisitSelect(SqlSelectExpression select)
			{
				var savedIsTopLevel = this.isTopLevel;

				this.isTopLevel = false;
				
				select = (SqlSelectExpression)base.VisitSelect(select);

				// Attempt to merge subqueries that would have been removed by the above
				// logic except for the existence of a where clause

				while (CanMergeWithFrom(select))
				{
					var fromSelect = select.From.GetLeftMostSelect();

					CanMergeWithFrom(select);

					// remove the redundant subquery
					select = SqlSubqueryRemover.Remove(select, fromSelect);

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
					var isDistinct = select.Distinct || fromSelect.Distinct;

					if (where != select.Where || orderBy != select.OrderBy || groupBy != select.GroupBy || isDistinct != select.Distinct || skip != select.Skip || take != select.Take)
					{
						select = new SqlSelectExpression(select.Type, select.Alias, select.Columns, select.From, where, orderBy, groupBy, isDistinct, skip, take, select.ForUpdate);
					}
				}

				this.isTopLevel = savedIsTopLevel;

				return select;
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

			private bool CanMergeWithFrom(SqlSelectExpression select)
			{
				var fromSelect = select.From.GetLeftMostSelect();

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

				var selHasNameMapProjection = SqlRedundantSubqueryFinder.IsNameMapProjection(select);
				var selHasSkip = select.Skip != null; 
				var selHasWhere = select.Where != null;
				var selHasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
				var selHasGroupBy = select.GroupBy != null && select.GroupBy.Count > 0;
				var selHasAggregates = SqlAggregateChecker.HasAggregates(select);
				var selHasJoin = select.From is SqlJoinExpression;
				var frmHasOrderBy = fromSelect.OrderBy != null && fromSelect.OrderBy.Count > 0;
				var frmHasGroupBy = fromSelect.GroupBy != null && fromSelect.GroupBy.Count > 0;
				var frmHasAggregates = SqlAggregateChecker.HasAggregates(fromSelect);

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

				// Cannot move forward GroupBy
				if (frmHasGroupBy)
				{
					return false;
				}

				// Cannot move forward a take if outer has take or skip or distinct
				if (fromSelect.Take != null && (select.Take != null || selHasSkip  || select.Distinct || selHasAggregates || selHasGroupBy || selHasJoin))
				{
					return false;
				}

				// Cannot move forward a skip if outer has skip or distinct or aggregates with accompanying groupby or where
				if (fromSelect.Skip != null && (select.Skip != null || select.Distinct || selHasAggregates || selHasGroupBy || selHasJoin))
				{
					return false;
				}

				// Cannot merge a distinct if the outer has a distinct or aggregates with accompanying groupby or where
				if (fromSelect.Distinct && (!selHasNameMapProjection || (((selHasWhere || selHasGroupBy) && (selHasAggregates || selHasOrderBy)))))
				{
					return false;
				}

				// cannot move forward a distinct if outer has take, skip, groupby or a different projection
				if (fromSelect.Distinct && (select.Take != null || select.Skip != null || !selHasNameMapProjection || selHasGroupBy || selHasAggregates || (selHasOrderBy && !this.isTopLevel) || selHasJoin))
				{
					return false;
				}

				if (frmHasAggregates && (select.Take != null || select.Skip != null || select.Distinct || selHasAggregates || selHasGroupBy || selHasJoin))
				{
					return false;
				}

				return true;
			}
		}
	}
}
