// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerLimitEmulationAmender
		: SqlExpressionVisitor
	{
		private const string RowColumnName = "__$$ROW_NUMBER";

		public static Expression Amend(Expression expression)
		{
			return new SqlServerLimitEmulationAmender().Visit(expression);
		}

		private static bool IsColumnAndAlreadyProjected(Expression expression, HashSet<string> aliases)
		{
			var columnExpression = expression as SqlColumnExpression;

			return aliases?.Contains(columnExpression?.AliasedName) ?? false;
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null)
			{
				const string orderByColumnPrefix = "__$$ORDERBYCOL";

				var rowNumber = new SqlFunctionCallExpression(typeof(int), "ROW_NUMBER");
				var selectWithRowAlias = selectExpression.Alias + "_ROW";

				var cols = selectExpression
					.Columns
					.Select(c => new SqlColumnDeclaration(c.Name, new SqlColumnExpression(c.Expression.Type, selectExpression.Alias, c.Name)))
					.ToList();

				var aliasedNames = selectExpression.OrderBy == null ? null : new HashSet<string>(selectExpression
					.Columns
					.Select(c => c.Expression)
					.OfType<SqlColumnExpression>()
					.Select(c => c.AliasedName));

				var orderByCols = selectExpression
					.OrderBy?
					.Where(c => !IsColumnAndAlreadyProjected(c.Expression, aliasedNames))
					.Select((c, i) => new SqlColumnDeclaration(orderByColumnPrefix + i, c.Expression)) ?? Enumerable.Empty<SqlColumnDeclaration>();
				
				SqlOverExpression over;

				if (selectExpression.OrderBy?.Any() == true)
				{
					var i = 0;

					over = new SqlOverExpression(rowNumber, selectExpression
						.OrderBy
						.Select(c =>
						{
							if (IsColumnAndAlreadyProjected(c.Expression, aliasedNames))
							{
								return new SqlOrderByExpression(c.OrderType, new SqlColumnExpression(c.Type, selectExpression.Alias, ((SqlColumnExpression)c.Expression).Name));
							}
							else
							{
								return new SqlOrderByExpression(c.OrderType, new SqlColumnExpression(c.Type, selectExpression.Alias, orderByColumnPrefix + i++));
							}
						}).ToReadOnlyCollection());
				}
				else
				{
					over = new SqlOverExpression(rowNumber, cols.Select(c => new SqlOrderByExpression(OrderType.Ascending, c.Expression)).ToReadOnlyCollection());
				}

				var innerSelect = orderByCols == null ? selectExpression : selectExpression.ChangeColumns(selectExpression.Columns.Concat(orderByCols));

				var rowColumn = new SqlColumnDeclaration(RowColumnName, over);

				cols.Add(rowColumn);

				var selectWithRowColumns = cols.ToReadOnlyCollection();
				var selectWithRow = new SqlSelectExpression(selectExpression.Type, selectWithRowAlias, selectWithRowColumns, Visit(innerSelect.ChangeOrderBy(null).ChangeSkipTake(null, null)), null, null, null, false, null, null, false);
				var outerColumns = selectExpression.Columns.Select(c => new SqlColumnDeclaration(c.Name, new SqlColumnExpression(c.Expression.Type, selectWithRowAlias, c.Name)));

				Expression rowPredicate = Expression.GreaterThan(new SqlColumnExpression(typeof(int), selectWithRowAlias, rowColumn.Name), selectExpression.Skip);

				if (selectExpression.Take != null && !(selectExpression.Take is SqlTakeAllValueExpression))
				{
					rowPredicate = Expression.And
					(
						rowPredicate,
						Expression.LessThanOrEqual(new SqlColumnExpression(typeof(int), selectWithRowAlias, rowColumn.Name), Expression.Add(selectExpression.Skip, selectExpression.Take))
					);
				}

				var retval = new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, outerColumns.ToReadOnlyCollection(), selectWithRow, rowPredicate, null, null, selectExpression.Distinct, null, null, selectExpression.ForUpdate, selectExpression.Reverse, selectExpression.Into);

				return retval;
			}

			return base.VisitSelect(selectExpression);
		}
	}
}
