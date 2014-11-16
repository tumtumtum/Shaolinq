// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.MySql
{
	/// <summary>
	/// If an auto-increment column is set explicitly by the user LAST_INSERT_ID will not return the expected value.
	/// This ammender makes sure the user defined value for the auto-increment column is returned by the next call
	/// to LAST_INSERT_ID.
	/// </summary>
	public class MySqlInsertIntoAutoIncrementAmmender
		: SqlExpressionVisitor
	{
		private MySqlInsertIntoAutoIncrementAmmender(SqlDataTypeProvider sqlDataTypeProvider)
		{
		}

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

					return new SqlInsertIntoExpression(expression.TableName, expression.ColumnNames, expression.ReturningAutoIncrementColumnNames, newValueExpressions);
				}
			}
			
			return expression;
		}

		public static Expression Ammend(Expression expression, SqlDataTypeProvider sqlDataTypeProvider)
		{
			var processor = new MySqlInsertIntoAutoIncrementAmmender(sqlDataTypeProvider);

			return processor.Visit(expression);
		}
	}
}
