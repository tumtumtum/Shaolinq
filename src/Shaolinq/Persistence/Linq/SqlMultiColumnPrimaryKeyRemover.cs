// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class SqlMultiColumnPrimaryKeyRemover
		: SqlExpressionVisitor
	{
		public static Expression Remove(Expression expression)
		{
			return new SqlMultiColumnPrimaryKeyRemover().Visit(expression);
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var count = 0;

			foreach (var columnDefinition in createTableExpression
				.ColumnDefinitionExpressions
				.Where(columnDefinition => columnDefinition
					.ConstraintExpressions
					.OfType<SqlSimpleConstraintExpression>()
				    .Any(simpleConstraint => simpleConstraint.Constraint == SqlSimpleConstraint.PrimaryKey)))
			{
				count++;

				if (count >= 2)
				{
					break;
				}
			}

			if (count < 2)
			{
				return base.VisitCreateTable(createTableExpression);	
			}

			var newColumnExpressions = new List<SqlColumnDefinitionExpression>();
			var newTableConstraintExpressions = new List<Expression>(createTableExpression.TableConstraints);

			foreach (var columnDefinition in createTableExpression.ColumnDefinitionExpressions)
			{
				var newConstraints = columnDefinition.ConstraintExpressions.Where(delegate(Expression constraint)
				{
					var simpleConstraint = constraint as SqlSimpleConstraintExpression;

					if (simpleConstraint == null)
					{
						return true;
					}

					return simpleConstraint.Constraint != SqlSimpleConstraint.PrimaryKey;
				});

				if (ReferenceEquals(newConstraints, columnDefinition.ConstraintExpressions))
				{
					newColumnExpressions.Add(columnDefinition);
				}
				else
				{
					newColumnExpressions.Add(new SqlColumnDefinitionExpression(columnDefinition.ColumnName, columnDefinition.ColumnType, newConstraints));
				}
			}

			return new SqlCreateTableExpression(createTableExpression.Table, false, newColumnExpressions, newTableConstraintExpressions, Enumerable.Empty<SqlTableOption>());
		}
	}
}
