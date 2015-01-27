using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Platform.Collections;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerBooleanNormalizer
		: SqlExpressionVisitor
	{
		public static Expression Normalize(Expression expression)
		{
			return new SqlServerBooleanNormalizer().Visit(expression);
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			var test = this.Visit(expression.Test);

			if (test.Type == typeof(string))
			{
				test = Expression.Condition(test, Expression.Constant("true"), Expression.Constant("false"));
			}

			var ifFalse = this.Visit(expression.IfFalse);

			if (ifFalse.Type == typeof(bool))
			{
				ifFalse = Expression.Condition(ifFalse, Expression.Constant("true"), Expression.Constant(("false")));
			}

			var ifTrue = this.Visit(expression.IfTrue);

			if (ifTrue.Type == typeof(bool))
			{
				ifTrue = Expression.Condition(ifTrue, Expression.Constant("true"), Expression.Constant(("false")));
			}

			if (test != expression.Test || ifFalse != expression.IfFalse || ifTrue != expression.IfTrue)
			{
				return Expression.Condition(test, ifTrue, ifFalse);
			}
			else
			{
				return base.VisitConditional(expression);
			}
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			List<SqlColumnDeclaration> newColumns = null;

			for (var i = 0; i < selectExpression.Columns.Count; i++)
			{
				var column = selectExpression.Columns[i];
				var visitedColumnExpression = this.Visit(column.Expression);

				if (visitedColumnExpression.Type.GetUnwrappedNullableType() == typeof(bool))
				{
					if (newColumns == null)
					{
						newColumns = new List<SqlColumnDeclaration>(selectExpression.Columns.Take(i));
					}

					var newColumnExpression = Expression.Condition(visitedColumnExpression, Expression.Constant("true"), Expression.Constant(("false")));

					var newColumnDeclaration = new SqlColumnDeclaration(column.Name, newColumnExpression);

					newColumns.Add(newColumnDeclaration);
				}
			}

			var where = this.Visit(selectExpression.Where);

			if (where != null && where.Type == typeof(string))
			{
				where = Expression.Equal(where, Expression.Constant("true"));
			}

			if (where != selectExpression.Where)
			{
				if (newColumns != null)
				{
					return selectExpression.ChangeWhereAndColumns(where, new ReadOnlyList<SqlColumnDeclaration>(newColumns));
				}
				else
				{
					return selectExpression.ChangeWhere(where);
				}
			}
			else if (newColumns != null)
			{
				return selectExpression.ChangeColumns(newColumns, true);
			}
			else
			{
				return base.VisitSelect(selectExpression);
			}
		}
	}
}
