// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;

namespace Shaolinq.SqlServer
{
	public class SqlServerLimitAmender
		: SqlExpressionVisitor
	{
		private const string RowColumnName = "$$ROW_NUMBER";

		public static Expression Amend(Expression expression)
		{
			return new SqlServerLimitAmender().Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null)
			{
				var rowNumber = new SqlFunctionCallExpression(typeof(int), "ROW_NUMBER");

				var oldAliases = (selectExpression.From as ISqlExposesAliases)?.Aliases;
				var innerSelectWithRowAlias = selectExpression.Alias + "_ROW";

				var cols = selectExpression.Columns.Select(c => new SqlColumnDeclaration(c.Name, new SqlColumnExpression(c.Expression.Type, selectExpression.Alias, c.Name))).ToList();
				var over = new SqlOverExpression(rowNumber, selectExpression.OrderBy?.ToReadOnlyCollection() ?? cols.Select(c => new SqlOrderByExpression(OrderType.Ascending, c.Expression)).ToReadOnlyCollection());

				if (oldAliases != null)
				{
					over = (SqlOverExpression)AliasReferenceReplacer.Replace(over, oldAliases.Contains, selectExpression.Alias);
				}

				var rowColumn = new SqlColumnDeclaration(RowColumnName, over);

				cols.Add(rowColumn);

				var innerSelectWithRowColumns = cols.ToReadOnlyCollection();
				var innerSelectWithRow = new SqlSelectExpression(selectExpression.Type, innerSelectWithRowAlias, innerSelectWithRowColumns, this.Visit(selectExpression.ChangeOrderBy(null).ChangeSkipTake(null, null)), null, null, null, false, null, null, false);
				var outerColumns = selectExpression.Columns.Select(c => new SqlColumnDeclaration(c.Name, new SqlColumnExpression(c.Expression.Type, innerSelectWithRowAlias, c.Name)));

				Expression rowPredicate = Expression.GreaterThan(new SqlColumnExpression(typeof(int), innerSelectWithRowAlias, rowColumn.Name), selectExpression.Skip);

				if (selectExpression.Take != null && !(selectExpression.Take is SqlTakeAllValueExpression))
				{
					rowPredicate = Expression.And
					(
						rowPredicate,
						Expression.LessThanOrEqual(new SqlColumnExpression(typeof(int), innerSelectWithRowAlias, rowColumn.Name), Expression.Add(selectExpression.Skip, selectExpression.Take))
					);
				}

				var retval = new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, outerColumns.ToReadOnlyCollection(), innerSelectWithRow, rowPredicate, null, null, selectExpression.Distinct, null, null, selectExpression.ForUpdate, selectExpression.Reverse, selectExpression.Into);

				return retval;
			}

			return base.VisitSelect(selectExpression);
		}
	}
}
