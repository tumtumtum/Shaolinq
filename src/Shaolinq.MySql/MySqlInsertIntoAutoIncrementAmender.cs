// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.MySql
{
	/// <summary>
	/// If an auto-increment column is set explicitly by the user LAST_INSERT_ID will not return the expected value.
	/// This amender makes sure the user defined value for the auto-increment column is returned by the next call
	/// to LAST_INSERT_ID.
	/// </summary>
	public class MySqlInsertIntoAutoIncrementAmender
		: SqlExpressionVisitor
	{
		private MySqlInsertIntoAutoIncrementAmender()
		{
		}

		public static Expression Amend(Expression expression) => new MySqlInsertIntoAutoIncrementAmender().Visit(expression);

		protected override Expression VisitInsertInto(SqlInsertIntoExpression expression)
		{
			if (expression.ReturningAutoIncrementColumnNames != null && expression.ReturningAutoIncrementColumnNames.Count == 1)
			{
				var returningColumnName = expression.ReturningAutoIncrementColumnNames[0];
				var index = expression.ColumnNames.IndexOf(returningColumnName);
				
				if (index > 0)
				{
					var newValueExpressions = new List<Expression>(expression.ValueExpressions);

					newValueExpressions[index] = new SqlFunctionCallExpression(newValueExpressions[index].Type, "LAST_INSERT_ID", newValueExpressions[index]);

					return new SqlInsertIntoExpression(expression.Source, expression.ColumnNames, expression.ReturningAutoIncrementColumnNames, newValueExpressions);
				}
			}
			
			return expression;
		}
	}
}
