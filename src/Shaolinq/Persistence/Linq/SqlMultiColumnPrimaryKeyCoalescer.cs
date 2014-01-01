// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class SqlMultiColumnPrimaryKeyCoalescer
		: SqlExpressionVisitor
	{
		public static Expression Coalesce(Expression expression)
		{
			return new SqlMultiColumnPrimaryKeyCoalescer().Visit(expression);
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var count = 0;

			foreach (SqlColumnDefinitionExpression columnDefinition in createTableExpression.ColumnDefinitionExpressions)
			{
				if (columnDefinition.ConstraintExpressions.OfType<SqlSimpleConstraintExpression>().Any(simpleConstraint => simpleConstraint.Constraint == SqlSimpleConstraint.PrimaryKey || simpleConstraint.Constraint == SqlSimpleConstraint.PrimaryKeyAutoIncrement))
				{
					count++;

					if (count >= 2)
					{
						break;
					}
				}
			}

			if (count < 2)
			{
				return base.VisitCreateTable(createTableExpression);	
			}

			var newColumnExpressions = new List<Expression>();
			var newTableConstraintExpressions = new List<Expression>(createTableExpression.TableConstraints);

			foreach (SqlColumnDefinitionExpression columnDefinition in createTableExpression.ColumnDefinitionExpressions)
			{
				var newConstraints = columnDefinition.ConstraintExpressions.FastWhere(delegate(Expression constraint)
				{
					var simpleConstraint = constraint as SqlSimpleConstraintExpression;

					if (simpleConstraint == null)
					{
						return true;
					}

					return !(simpleConstraint.Constraint == SqlSimpleConstraint.PrimaryKey || simpleConstraint.Constraint == SqlSimpleConstraint.PrimaryKeyAutoIncrement);
				});

				if (newConstraints == columnDefinition.ConstraintExpressions)
				{
					newColumnExpressions.Add(columnDefinition);
				}
				else
				{
					newColumnExpressions.Add(new SqlColumnDefinitionExpression(columnDefinition.ColumnName, columnDefinition.ColumnTypeName, newConstraints));
				}
			}

			return new SqlCreateTableExpression(createTableExpression.Table, newColumnExpressions, newTableConstraintExpressions);
		}
	}
}
