// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Platform;
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

				var oldAlias = (selectExpression.From as SqlAliasedExpression)?.Alias;

				var x = SqlOrderByRewriter.Rewrite(selectExpression);

				var innerSelectWithRowAlias = selectExpression.Alias + "_ROW";

				var over = new SqlOverExpression(rowNumber, selectExpression.OrderBy?.Cast<SqlOrderByExpression>().ToReadOnlyCollection() ?? selectExpression.Columns.Select(c => new SqlOrderByExpression(OrderType.Ascending, c.Expression)).ToReadOnlyCollection());

				over = (SqlOverExpression)AliasReferenceReplacer.Replace(over, oldAlias, selectExpression.Alias);

				var rowColumn = new SqlColumnDeclaration(RowColumnName, over);
				var innerSelectWithRowColumns = selectExpression.Columns.Select(c => new SqlColumnDeclaration(c.Name, new SqlColumnExpression(c.Expression.Type, selectExpression.Alias, c.Name))).Concat(rowColumn).ToReadOnlyCollection();
				var innerSelectWithRow = new SqlSelectExpression(selectExpression.Type, innerSelectWithRowAlias, innerSelectWithRowColumns, selectExpression.ChangeOrderBy(null).ChangeSkipTake(null, null), null, null, null, false, null, null, false);
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

				var newOrderBy = selectExpression
					.OrderBy?
					.Select(c => AliasReferenceReplacer.Replace(c, oldAlias, innerSelectWithRowAlias))
					.Concat(new SqlOrderByExpression(OrderType.Ascending, new SqlColumnExpression(typeof(int), innerSelectWithRowAlias, rowColumn.Name)))
					.ToReadOnlyCollection();

				var retval = new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, outerColumns, innerSelectWithRow, rowPredicate, newOrderBy, selectExpression.ForUpdate);

				return retval;
			}

			return base.VisitSelect(selectExpression);
		}
	}
}
