// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class RedundantSubqueryFinder
		: SqlExpressionVisitor
	{
		List<SqlSelectExpression> redundant;

		private RedundantSubqueryFinder()
		{
		}

		internal static List<SqlSelectExpression> Find(Expression source)
		{
			var gatherer = new RedundantSubqueryFinder();

			gatherer.Visit(source);

			return gatherer.redundant;
		}

		protected static bool IsInitialProjection(SqlSelectExpression select)
		{
			return select.From is SqlTableExpression;
		}

		internal static bool IsSimpleProjection(SqlSelectExpression select)
		{
			foreach (var decl in select.Columns)
			{
				var col = decl.Expression as SqlColumnExpression;

				if (col == null || decl.Name != col.Name)
				{
					return false;
				}
			}

			return true;
		}

		internal static bool IsNameMapProjection(SqlSelectExpression select)
		{
			if (select.From is SqlTableExpression)
			{
				return false;
			}

			var fromSelect = select.From as SqlSelectExpression;

			if (fromSelect == null || select.Columns.Count > fromSelect.Columns.Count)
			{
				return false;
			}

			var fromColumnNames = new HashSet<string>(fromSelect.Columns.Select(x => x.Name));

			foreach (var t in @select.Columns)
			{
				var columnExpression = t.Expression as SqlColumnExpression;

				if (columnExpression == null || !fromColumnNames.Contains(columnExpression.Name))
				{
					return false;
				}
			}

			return true;
		}

		private static bool IsRedudantSubquery(SqlSelectExpression select)
		{
			return (IsSimpleProjection(select) || IsNameMapProjection(select))
				&& !select.Distinct
				&& (select.Take == null)
                && (select.Skip == null)
				&& select.Where == null
                && !select.Columns.Any(c => c.NoOptimise)
				&& ((select.OrderBy?.Count ?? 0) == 0)
				&& ((select.GroupBy?.Count ?? 0) == 0);
		}

	    private readonly HashSet<Expression> ignoreSet = new HashSet<Expression>();

	    protected override Expression VisitJoin(SqlJoinExpression join)
	    {
            ignoreSet.Add(join.Left);
            ignoreSet.Add(join.Right);

            var left = this.Visit(join.Left);
            var right = this.Visit(join.Right);
            
            ignoreSet.Remove(join.Left);
            ignoreSet.Remove(join.Right);

            var condition = this.Visit(join.JoinCondition);

            if (left != join.Left || right != join.Right || condition != join.JoinCondition)
            {
                return new SqlJoinExpression(join.Type, join.JoinType, left, right, condition);
            }

            return join;
        }

		protected override Expression VisitDelete(SqlDeleteExpression deleteExpression)
		{
			return deleteExpression;
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
	        if (ignoreSet.Contains(select))
	        {
	            return base.VisitSelect(select);
	        }

			if (IsRedudantSubquery(select))
			{
				if (this.redundant == null)
				{
					this.redundant = new List<SqlSelectExpression>();
				}

				this.redundant.Add(select);
			}

			return base.VisitSelect(select);
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			return subquery;
		}
	}
}
