// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
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
		private bool outerCanReceiveOrderings;
		private IEnumerable<SqlOrderByExpression> gatheredOrderings;

		private SqlOrderByRewriter()
		{
			this.outerCanReceiveOrderings = true;
		}

		public static Expression Rewrite(Expression expression)
		{
			var orderByRewriter = new SqlOrderByRewriter();

			return orderByRewriter.Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			var canHaveOrderBy = select.Take != null || select.Skip != null;
			var hasGroupBy = select.GroupBy?.Count > 0;
			var canReceiveOrderings = canHaveOrderBy && !hasGroupBy && !select.Distinct && !SqlAggregateChecker.HasAggregates(select);

			this.outerCanReceiveOrderings &= canReceiveOrderings;

			select = (SqlSelectExpression)base.VisitSelect(select);

			var hasOrderBy = select.OrderBy?.Count > 0;
				
			if (hasOrderBy)
			{
				this.PrependOrderings(select.OrderBy);
			}
				
			var columns = select.Columns;
			var orderings = canReceiveOrderings ? this.gatheredOrderings : select.OrderBy;
			var canPassOnOrderings = this.outerCanReceiveOrderings && !hasGroupBy && !select.Distinct;

			if (this.gatheredOrderings != null)
			{
				if (canPassOnOrderings)
				{
					var producedAliases = SqlAliasesProduced.Gather(select.From);
					var project = this.RebindOrderings(this.gatheredOrderings, select.Alias, producedAliases, select.Columns);

					this.gatheredOrderings = null;
					this.PrependOrderings(project.Orderings);

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
		
		private void PrependOrderings(IEnumerable<SqlOrderByExpression> newOrderings)
		{
			if (newOrderings != null)
			{
				if (this.gatheredOrderings == null)
				{
					this.gatheredOrderings = newOrderings;
				}
				else
				{
					var orderExpressions = this.gatheredOrderings as List<SqlOrderByExpression>;

					if (orderExpressions == null)
					{
						this.gatheredOrderings = orderExpressions = new List<SqlOrderByExpression>(this.gatheredOrderings);
					}
					orderExpressions.InsertRange(0, newOrderings);
				}
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
						var declColumn = decl.Expression as SqlColumnExpression;

						if ((column != null && decl.Expression == ordering.Expression) ||
							(column != null && declColumn != null && column.SelectAlias == declColumn.SelectAlias && column.Name == declColumn.Name))
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

						newColumns.Add(new SqlColumnDeclaration(colName, ordering.Expression));
						expr = new SqlColumnExpression(expr.Type, alias, colName);
					}

					newOrderings.Add(new SqlOrderByExpression(ordering.OrderType, expr));
				}
			}

			return new BindResult(existingColumns, newOrderings);
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
