// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Move OrderBy expressions to the outer most select
	/// </summary>
	public class SqlOrderByRewriter
		: SqlExpressionVisitor
	{
		private bool isOuterMostSelect;
		private List<SqlOrderByExpression> gatheredOrderings;

		private SqlOrderByRewriter()
		{
			this.isOuterMostSelect = true;
		}

		public static Expression Rewrite(Expression expression)
		{
			return new SqlOrderByRewriter().Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			var saveIsOuterMostSelect = this.isOuterMostSelect;

			try
			{
				this.isOuterMostSelect = false;

				select = (SqlSelectExpression)base.VisitSelect(select);

				var canHaveOrderBy = saveIsOuterMostSelect || select.Take != null || select.Skip != null;
				var hasGroupBy = select.GroupBy?.Count > 0;
				var canReceiveOrderings = canHaveOrderBy && !hasGroupBy && !select.Distinct && !SqlAggregateChecker.HasAggregates(select);

				var hasOrderBy = select.OrderBy?.Count > 0;

				if (hasOrderBy)
				{
					PrependOrderings(select.OrderBy);
				}

				var columns = select.Columns;
				var orderings = canReceiveOrderings ? this.gatheredOrderings : (canHaveOrderBy ? select.OrderBy : null);
				var canPassOnOrderings = !saveIsOuterMostSelect && !hasGroupBy && !select.Distinct && (select.Take == null && select.Skip == null);

				if (this.gatheredOrderings != null)
				{
					if (canPassOnOrderings)
					{
						var producedAliases = SqlAliasesProduced.Gather(select.From);
						var project = RebindOrderings(this.gatheredOrderings, select.Alias, producedAliases, select.Columns);

						this.gatheredOrderings = null;
						PrependOrderings(project.Orderings);

						columns = project.Columns;
					}
					else
					{
						this.gatheredOrderings = null;
					}
				}

				if (orderings != select.OrderBy || columns != select.Columns)
				{
					select = new SqlSelectExpression(select.Type, select.Alias, columns, select.From, select.Where, orderings, select.GroupBy, select.Distinct, select.Skip, select.Take, select.ForUpdate);
				}

				return select;
			}
			finally
			{
				this.isOuterMostSelect = saveIsOuterMostSelect;
			}
		}
		
		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			var left = VisitSource(join.Left);
			var leftOrders = this.gatheredOrderings;
			
			this.gatheredOrderings = null;

			var right = VisitSource(join.Right);
			
			PrependOrderings(leftOrders);
			
			var condition = Visit(join.JoinCondition);

			if (left != join.Left || right != join.Right || condition != join.JoinCondition)
			{
				return new SqlJoinExpression(join.Type, join.JoinType, left, right, condition);
			}

			return join;
		}
		
		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			var saveOrderings = this.gatheredOrderings;
			
			this.gatheredOrderings = null;
			var result = base.VisitSubquery(subquery);

			this.gatheredOrderings = saveOrderings;

			return result;
		}

		private void PrependOrderings(IReadOnlyList<SqlOrderByExpression> newOrderings)
		{
			if (newOrderings == null)
			{
				return;
			}

			if (this.gatheredOrderings == null)
			{
				this.gatheredOrderings = new List<SqlOrderByExpression>();
			}

			this.gatheredOrderings.InsertRange(0, newOrderings);

			// Insert removing duplicates

			var  unique = new HashSet<string>();
			List<SqlOrderByExpression> newList = null;

			for (var i = 0; i < this.gatheredOrderings.Count; i++) 
			{
				if (this.gatheredOrderings[i].Expression is SqlColumnExpression column)
				{
					var hash = column.AliasedName;

					if (unique.Contains(hash))
					{
						if (newList == null)
						{
							newList = new List<SqlOrderByExpression>(this.gatheredOrderings.Take(i));
						}

						continue;
					}
					else
					{
						unique.Add(hash);
					}
				}

				newList?.Add(this.gatheredOrderings[i]);
			}

			if (newList != null)
			{
				this.gatheredOrderings = newList;
			}
		}

		protected class BindResult
		{
			public IReadOnlyList<SqlColumnDeclaration> Columns { get; }
			public IReadOnlyList<SqlOrderByExpression> Orderings { get; }

			public BindResult(IEnumerable<SqlColumnDeclaration> columns, IEnumerable<SqlOrderByExpression> orderings)
			{
				this.Columns = columns.ToReadOnlyCollection();
				this.Orderings = orderings.ToReadOnlyCollection();
			}
		}
		
		protected virtual BindResult RebindOrderings(IEnumerable<SqlOrderByExpression> orderings, string alias, HashSet<string> existingAliases, IReadOnlyList<SqlColumnDeclaration> existingColumns)
		{
			List<SqlColumnDeclaration> newColumns = null;
			
			var newOrderings = new List<SqlOrderByExpression>();

			foreach (var ordering in orderings)
			{
				var expr = ordering.Expression;
				var column = expr as SqlColumnExpression;

				if (column == null || (existingAliases != null && existingAliases.Contains(column.SelectAlias)))
				{	
					var ordinal = 0;

					// If a declared column already contains a similar expression then make a reference to that column

					foreach (var decl in existingColumns)
					{
						if ((column != null && decl.Expression == ordering.Expression) ||
							(column != null && decl.Expression is SqlColumnExpression declColumn && column.SelectAlias == declColumn.SelectAlias && column.Name == declColumn.Name))
						{
							expr = new SqlColumnExpression(column.Type, alias, decl.Name);

							break;
						}

						ordinal++;
					}
					
					if (expr == ordering.Expression)
					{
						if (newColumns == null)
						{
							newColumns = new List<SqlColumnDeclaration>(existingColumns);

							existingColumns = newColumns;
						}

						var colName = column != null ? column.Name : "COL" + ordinal;
						colName = GetAvailableColumnName(newColumns, colName);

						newColumns.Add(new SqlColumnDeclaration(colName, ordering.Expression));
						expr = new SqlColumnExpression(expr.Type, alias, colName);
					}

					newOrderings.Add(new SqlOrderByExpression(ordering.OrderType, expr));
				}
			}

			return new BindResult(existingColumns, newOrderings);
		}

		public static string GetAvailableColumnName(IList<SqlColumnDeclaration> columns, string baseName)
		{
			var n = 0;
			var name = baseName;

			while (columns.Any(col => col.Name == name))
			{
				name = baseName + (n++);
			}

			return name;
		}

		private class SqlAliasesProduced
			: SqlExpressionVisitor
		{
			private readonly HashSet<string> aliases;

			private SqlAliasesProduced()
			{
				this.aliases = new HashSet<string>();
			}

			public static HashSet<string> Gather(Expression source)
			{
				var aliasesProduced = new SqlAliasesProduced();

				aliasesProduced.Visit(source);

				return aliasesProduced.aliases;
			}

			protected override Expression VisitSelect(SqlSelectExpression select)
			{
				this.aliases.Add(select.Alias);

				return select;
			}

			protected override Expression VisitTable(SqlTableExpression table)
			{
				this.aliases.Add(table.Alias);

				return table;
			}
		}
	}
}
