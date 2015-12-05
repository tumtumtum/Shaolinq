// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Removes column declarations in selects that are not referenced
	/// </summary>
	public class SqlUnusedColumnRemover
		: SqlExpressionVisitor
	{
		private readonly Dictionary<string, HashSet<string>> allColumnsUsed;

		private SqlUnusedColumnRemover()
		{
			this.allColumnsUsed = new Dictionary<string, HashSet<string>>();
		}

		public static Expression Remove(Expression expression)
		{
			return new SqlUnusedColumnRemover().Visit(expression);
		}

		private void MarkColumnAsUsed(string alias, string name)
		{
			HashSet<string> columns;

			if (!this.allColumnsUsed.TryGetValue(alias, out columns))
			{
				columns = new HashSet<string>();
			
				this.allColumnsUsed.Add(alias, columns);
			}

			columns.Add(name);
		}

		private bool IsColumnUsed(string alias, string name)
		{
			HashSet<string> columnsUsed;

			if (this.allColumnsUsed.TryGetValue(alias, out columnsUsed))
			{
				if (columnsUsed != null)
				{
					return columnsUsed.Contains(name);
				}
			}

			return false;
		}

		private void ClearColumnsUsed(string alias)
		{
			this.allColumnsUsed[alias] = new HashSet<string>();
		}

		protected override Expression VisitColumn(SqlColumnExpression column)
		{
			this.MarkColumnAsUsed(column.SelectAlias, column.Name);

			return column;
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			if (subquery.Select != null)
			{
				Debug.Assert(subquery.Select.Columns.Count == 1);

				this.MarkColumnAsUsed(subquery.Select.Alias, subquery.Select.Columns[0].Name);
			}

			return base.VisitSubquery(subquery);
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			// Visit column projection first

			var columns = select.Columns;

			List<SqlColumnDeclaration> alternate = null;

			for (int i = 0, n = select.Columns.Count; i < n; i++)
			{
				var columnDeclaration = select.Columns[i];

				if (select.Distinct || this.IsColumnUsed(select.Alias, columnDeclaration.Name))
				{
					var expr = this.Visit(columnDeclaration.Expression);

					if (expr != columnDeclaration.Expression)
					{
						columnDeclaration = new SqlColumnDeclaration(columnDeclaration.Name, expr);
					}
				}
				else
				{
					columnDeclaration = null;  // null means it gets omitted
				}

				if (columnDeclaration != select.Columns[i] && alternate == null)
				{
					alternate = new List<SqlColumnDeclaration>();

					for (var j = 0; j < i; j++)
					{
						alternate.Add(select.Columns[j]);
					}
				}

				if (columnDeclaration != null && alternate != null)
				{
					alternate.Add(columnDeclaration);
				}
			}

			if (alternate != null)
			{
				columns = alternate.ToReadOnlyCollection();
			}

			var take = this.Visit(select.Take);
			var skip = this.Visit(select.Skip);
			var groupbys = this.VisitExpressionList(select.GroupBy);
			var orderbys = this.VisitExpressionList(select.OrderBy);
			var where = this.Visit(select.Where);
			var from = this.Visit(select.From);

			this.ClearColumnsUsed(select.Alias);

			if (columns != select.Columns
				|| orderbys != select.OrderBy
				|| groupbys != select.GroupBy
				|| where != select.Where
				|| from != select.From
				|| take != select.Take
				|| skip != select.Skip)
			{
				select = new SqlSelectExpression(select.Type, select.Alias, columns, from, where, orderbys, groupbys, select.Distinct, skip, take, select.ForUpdate);
			}

			return select;
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			// Visit mapping in reverse order

			var projector = this.Visit(projection.Projector);
			var select = (SqlSelectExpression)this.Visit(projection.Select);

			return UpdateProjection(projection, select, projector, projection.Aggregator);
		}
        
		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			// Visit join in reverse order

			var condition = this.Visit(join.JoinCondition);
			var right = this.VisitSource(join.Right);
			var left = this.VisitSource(join.Left);

			if (left != join.Left || right != join.Right || condition != join.JoinCondition)
			{
				return new SqlJoinExpression(join.Type, join.JoinType, left, right, condition);
			}

			return join;
		}
	}
}
