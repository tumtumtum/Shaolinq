// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform.Collections;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerLimitAmmender
		: SqlExpressionVisitor
	{
		private const string RowColumnName = "Row";

		public static Expression Ammend(Expression expression)
		{
			return new SqlServerLimitAmmender().Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null)
			{
				var rowNumber = new SqlFunctionCallExpression(typeof(int), "ROW_NUMBER");
				var over = new SqlOverExpression(rowNumber, selectExpression.OrderBy ?? new ReadOnlyList<Expression>(new [] { new SqlOrderByExpression(OrderType.Ascending, selectExpression.Columns[0].Expression) }));
				var additionalColumn = new SqlColumnDeclaration(RowColumnName, over);

				var newAlias = selectExpression.Alias + "INNER";
				var innerColumns = new ReadOnlyList<SqlColumnDeclaration>(selectExpression.Columns.Select(c => c).Concat(new[] { additionalColumn }));
				
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
