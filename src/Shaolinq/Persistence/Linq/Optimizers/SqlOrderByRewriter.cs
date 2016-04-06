// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
		private IEnumerable<SqlOrderByExpression> gatheredOrderings;

		private SqlOrderByRewriter()
		{
			this.isOuterMostSelect = true;
		}

		public static Expression Rewrite(Expression expression)
		{
			var orderByRewriter = new SqlOrderByRewriter();

			return orderByRewriter.Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			var saveIsOuterMostSelect = this.isOuterMostSelect;

			try
			{
				this.isOuterMostSelect = false;

				select = (SqlSelectExpression)base.VisitSelect(select);

				var hasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;

				if (hasOrderBy)
				{
					this.PrependOrderings(select.OrderBy.Select(c => (SqlOrderByExpression)c));
				}

				var canHaveOrderBy = saveIsOuterMostSelect && !SqlExpressionFinder.FindExists(select, c => c.NodeType == (ExpressionType)SqlExpressionType.Aggregate || c.NodeType == (ExpressionType)SqlExpressionType.AggregateSubquery);
				var canPassOnOrderings = !saveIsOuterMostSelect;

				var columns = select.Columns;
				IEnumerable<Expression> orderings = (canHaveOrderBy) ? this.gatheredOrderings : null;
				
				if (this.gatheredOrderings != null)
				{
					if (canPassOnOrderings)
					{
						var producedAliases = AliasesProduced.Gather(select.From);
						var project = this.RebindOrderings(this.gatheredOrderings, select.Alias, producedAliases, select.Columns);

						this.gatheredOrderings = project.Orderings;

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

		/// <summary>
		/// Add a sequence of order expressions to an accumulated list, prepending so as
		/// to give precedence to the new expressions over any previous expressions
		/// </summary>
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

		/// <summary>
		/// Rebind order expressions to reference a new alias and add to column declarations if necessary
		/// </summary>
		protected virtual BindResult RebindOrderings(IEnumerable<SqlOrderByExpression> orderings, string alias, HashSet<string> existingAliases, IEnumerable<SqlColumnDeclaration> existingColumns)
		{
			List<SqlColumnDeclaration> newColumns = null;
			
			var newOrderings = new List<SqlOrderByExpression>();

			foreach (var ordering in orderings)
			{
				var expr = ordering.Expression;
				var column = expr as SqlColumnExpression;

				if (column == null || (existingAliases != null && existingAliases.Contains(column.SelectAlias)))
				{
					// Check to see if a declared column already contains a similar expression
					var ordinal = 0;

					foreach (var decl in existingColumns)
					{
						var declColumn = decl.Expression as SqlColumnExpression;

						if ((column != null && decl.Expression == ordering.Expression) ||
							(column != null && declColumn != null && column.SelectAlias == declColumn.SelectAlias && column.Name == declColumn.Name))
						{
							// Found it, so make a reference to this column
							expr = new SqlColumnExpression(column.Type, alias, decl.Name);

							break;
						}

						ordinal++;
					}

					// If not already projected, add a new column declaration for it
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

		/// <summary>
		///  Returns the set of all aliases produced by a query source
		/// </summary>
		private class AliasesProduced
			: SqlExpressionVisitor
		{
			private readonly HashSet<string> aliases;

			private AliasesProduced()
			{
				this.aliases = new HashSet<string>();
			}

			public static HashSet<string> Gather(Expression source)
			{
				var aliasesProduced = new AliasesProduced();

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
