// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Platform.Collections;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerLimitAmender
		: SqlExpressionVisitor
	{
		private const string RowColumnName = "Row";

		public static Expression Amend(Expression expression)
		{
			return new SqlServerLimitAmender().Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null)
			{
				var rowNumber = new SqlFunctionCallExpression(typeof(int), "ROW_NUMBER");
				var over = new SqlOverExpression(rowNumber, (selectExpression.OrderBy ?? new [] { new SqlOrderByExpression(OrderType.Ascending, selectExpression.Columns[0].Expression) }).ToReadOnlyCollection());
				var additionalColumn = new SqlColumnDeclaration(RowColumnName, over);

				var newAlias = selectExpression.Alias + "INNER";
				var innerColumns = selectExpression.Columns.Select(c => c).Concat(new[] { additionalColumn }).ToReadOnlyCollection();
				
				var innerSelect = new SqlSelectExpression(selectExpression.Type, newAlias, innerColumns, selectExpression.From, selectExpression.Where, null, selectExpression.GroupBy, selectExpression.Distinct, null, null, selectExpression.ForUpdate);

				var outerColumns = selectExpression.Columns.Select(c => new SqlColumnDeclaration(c.Name, new SqlColumnExpression(c.Expression.Type, newAlias, c.Name)));

				Expression rowPredicate = Expression.GreaterThan(new SqlColumnExpression(typeof(int), newAlias, additionalColumn.Name), selectExpression.Skip);

				if (selectExpression.Take != null && !(selectExpression.Take is SqlTakeAllValueExpression))
				{
					rowPredicate = Expression.And
					(
						rowPredicate,
						Expression.LessThanOrEqual(new SqlColumnExpression(typeof(int), newAlias, additionalColumn.Name), Expression.Add(selectExpression.Skip, selectExpression.Take))
					);
				}

				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, outerColumns, innerSelect, rowPredicate, null, selectExpression.ForUpdate);
			}

			return base.VisitSelect(selectExpression);
		}
	}
}
