// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
				.Where(c => c
					.ConstraintExpressions
					.Any(d => d.SimpleConstraint == SqlSimpleConstraint.PrimaryKey)))
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
			var newTableConstraintExpressions = new List<SqlConstraintExpression>(createTableExpression.TableConstraints);

			foreach (var columnDefinition in createTableExpression.ColumnDefinitionExpressions)
			{
				var newConstraints = columnDefinition.ConstraintExpressions.Where(c => c.SimpleConstraint != SqlSimpleConstraint.PrimaryKey).ToReadOnlyCollection();

				newColumnExpressions.Add(new SqlColumnDefinitionExpression(columnDefinition.ColumnName, columnDefinition.ColumnType, newConstraints));
			}

			return new SqlCreateTableExpression(createTableExpression.Table, false, newColumnExpressions, newTableConstraintExpressions, Enumerable.Empty<SqlTableOption>().ToReadOnlyCollection());
		}
	}
}
