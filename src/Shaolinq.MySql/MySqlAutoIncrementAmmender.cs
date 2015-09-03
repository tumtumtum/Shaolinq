using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Platform.Collections;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.MySql
{
	public class MySqlAutoIncrementAmmender
		: SqlExpressionVisitor
	{
		private MySqlAutoIncrementAmmender()
		{
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var autoIncrementColumn = createTableExpression.ColumnDefinitionExpressions.SingleOrDefault(c => c.ConstraintExpressions.OfType<SqlSimpleConstraintExpression>().Any(d => d.Constraint == SqlSimpleConstraint.AutoIncrement));

			if (autoIncrementColumn != null)
			{
				var primaryKeyConstraint = createTableExpression.TableConstraints.OfType<SqlSimpleConstraintExpression>().SingleOrDefault(c => c.Constraint == SqlSimpleConstraint.PrimaryKey);

				if (primaryKeyConstraint != null)
				{
					if (!primaryKeyConstraint.ColumnNames.Contains(autoIncrementColumn.ColumnName))
					{
						var newPrimaryKeyConstraint = new SqlSimpleConstraintExpression(SqlSimpleConstraint.PrimaryKey, new [] { autoIncrementColumn.ColumnName });
						var newUniqueConstraint = new SqlSimpleConstraintExpression(SqlSimpleConstraint.Unique, primaryKeyConstraint.ColumnNames.Concat(autoIncrementColumn.ColumnName).ToArray());

						return createTableExpression.UpdateConstraints(new ReadOnlyList<Expression>(createTableExpression.TableConstraints.Where(c => c != primaryKeyConstraint).Concat(newPrimaryKeyConstraint).Concat(newUniqueConstraint)));
					}
				}
				else
				{
					var newPrimaryKeyConstraint = new SqlSimpleConstraintExpression(SqlSimpleConstraint.PrimaryKey, new [] { autoIncrementColumn.ColumnName });

					return createTableExpression.UpdateConstraints(new ReadOnlyList<Expression>(newPrimaryKeyConstraint));
				}
			}

			return base.VisitCreateTable(createTableExpression);
		}

		public static Expression Ammend(Expression expression)
		{
			var processor = new MySqlAutoIncrementAmmender();

			return processor.Visit(expression);
		}
	}
}
