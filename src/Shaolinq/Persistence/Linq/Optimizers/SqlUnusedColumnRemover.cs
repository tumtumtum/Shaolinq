// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
			alias = alias ?? "";

			if (!this.allColumnsUsed.TryGetValue(alias, out var columns))
			{
				columns = new HashSet<string>();
			
				this.allColumnsUsed.Add(alias, columns);
			}

			columns.Add(name);
		}

		private bool IsColumnUsed(string alias, string name)
		{
			if (this.allColumnsUsed.TryGetValue(alias, out var columnsUsed))
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

		private void MarkAllColumnsUsed(string alias, IEnumerable<string> columns)
		{
			this.allColumnsUsed[alias] = new HashSet<string>(columns);
		}

		protected override Expression VisitColumn(SqlColumnExpression column)
		{
			MarkColumnAsUsed(column.SelectAlias, column.Name);

			return column;
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			if (unaryExpression.Operand is SqlSelectExpression select)
			{
				MarkAllColumnsUsed(select.Alias, select.Columns.Select(c => c.Name));
			}

			return base.VisitUnary(unaryExpression);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			foreach (var select in functionCallExpression.Arguments.OfType<SqlSelectExpression>())
			{
				MarkAllColumnsUsed(select.Alias, select.Columns.Select(c => c.Name));
			}

			return base.VisitFunctionCall(functionCallExpression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			// If a query is used in a comparison then we can't assume the exact number of columns 
			// selected isn't needed for the comparison 
			
			if (binaryExpression.Left is SqlSelectExpression select1)
			{
				MarkAllColumnsUsed(select1.Alias, select1.Columns.Select(c => c.Name));
			}

			if (binaryExpression.Right is SqlSelectExpression select2)
			{
				MarkAllColumnsUsed(select2.Alias, select2.Columns.Select(c => c.Name));
			}

			return base.VisitBinary(binaryExpression);
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			if (subquery.Select != null)
			{
				Debug.Assert(subquery.Select.Columns.Count == 1);

				MarkColumnAsUsed(subquery.Select.Alias, subquery.Select.Columns[0].Name);
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

				if (select.Distinct || IsColumnUsed(select.Alias, columnDeclaration.Name) || columnDeclaration.NoOptimise)
				{
					var expr = Visit(columnDeclaration.Expression);

					if (expr != columnDeclaration.Expression)
					{
						columnDeclaration = new SqlColumnDeclaration(columnDeclaration.Name, expr);
					}
				}
				else
				{
					// null means it gets omitted

					columnDeclaration = null; 
				}

				if (columnDeclaration != select.Columns[i] && alternate == null)
				{
					alternate = new List<SqlColumnDeclaration>();

					for (var j = 0; j < i; j++)
					{
						alternate.Add(select.Columns[j]);
					}
				}

				if (columnDeclaration != null)
				{
					alternate?.Add(columnDeclaration);
				}
			}

			if (alternate != null)
			{
				// Conservatively, don't remove any columns if it would mean removing all of them

				if (alternate.Count == 0)
				{
					// Visit all columns so that their references/expressions can be marked as used

					foreach (var c in select.Columns)
					{
						Visit(c.Expression);
					}
				}
				else
				{
					columns = alternate.ToReadOnlyCollection();
				}
			}

			var take = Visit(select.Take);
			var skip = Visit(select.Skip);
			var groupbys = VisitExpressionList(select.GroupBy);
			var orderbys = VisitExpressionList(select.OrderBy);
			var where = Visit(select.Where);
			var from = Visit(select.From);

			ClearColumnsUsed(select.Alias);

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

			// This collects columns that are used in the projector
			var projector = Visit(projection.Projector);
		
			// Removes columns that aren't used
			var select = (SqlSelectExpression)Visit(projection.Select);

			return UpdateProjection(projection, select, projector, projection.Aggregator);
		}
		
		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			// Visit join in reverse order

			var condition = Visit(join.JoinCondition);
			var right = VisitSource(join.Right);
			var left = VisitSource(join.Left);

			if (left != join.Left || right != join.Right || condition != join.JoinCondition)
			{
				return new SqlJoinExpression(join.Type, join.JoinType, left, right, condition);
			}

			return join;
		}
	}
}
