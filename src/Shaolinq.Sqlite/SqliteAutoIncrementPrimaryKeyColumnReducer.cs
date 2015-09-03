// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using Platform;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Sqlite
{
	public class SqliteAutoIncrementPrimaryKeyColumnReducer
		: SqlExpressionVisitor
	{
		private HashSet<string> columnsToMakeNotNull = new HashSet<string>();
		private readonly IDictionary<string, string> primaryKeyNameByTablesWithReducedPrimaryKeyName = new Dictionary<string, string>();
		
		private SqliteAutoIncrementPrimaryKeyColumnReducer()
		{
		}

		public static Expression Reduce(Expression expression, out IDictionary<string, string> primaryKeyNameByTablesWithReducedPrimaryKeyName)
		{
			var reducer = new SqliteAutoIncrementPrimaryKeyColumnReducer();

			primaryKeyNameByTablesWithReducedPrimaryKeyName = reducer.primaryKeyNameByTablesWithReducedPrimaryKeyName;

			return reducer.Visit(expression);
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var primaryKeyConstraint = createTableExpression
				.TableConstraints
				.OfType<SqlSimpleConstraintExpression>()
				.SingleOrDefault(c => c.Constraint == SqlSimpleConstraint.PrimaryKey);

			if (primaryKeyConstraint != null)
			{
				var autoIncrementColumns = createTableExpression
					.ColumnDefinitionExpressions
					.SelectMany(c => c.ConstraintExpressions.OfType<SqlSimpleConstraintExpression>().Select(d => new { Constraint = d, ColumnDefinition = c}))
					.Where(c => c.Constraint.Constraint == SqlSimpleConstraint.AutoIncrement)
					.ToList();

				if (autoIncrementColumns.Count > 1)
				{
					throw new UnsupportedDataAccessModelDefinitionException();
				}

				if (autoIncrementColumns.Count > 0)
				{
					var autoIncrementColumn = autoIncrementColumns.Single();

					var newTableConstraints = createTableExpression
						.TableConstraints
						.Where(c => c != primaryKeyConstraint);

					if (primaryKeyConstraint.ColumnNames.Length > 1
						|| autoIncrementColumn.ColumnDefinition.ConstraintExpressions.OfType<SqlSimpleConstraintExpression>().All(c => c.Constraint != SqlSimpleConstraint.PrimaryKey))
					{
						var uniqueConstraint = new SqlSimpleConstraintExpression(SqlSimpleConstraint.Unique, primaryKeyConstraint.ColumnNames);

						newTableConstraints = newTableConstraints.Concat(uniqueConstraint);
					}

					primaryKeyNameByTablesWithReducedPrimaryKeyName[createTableExpression.Table.Name] = autoIncrementColumn.ColumnDefinition.ColumnName;

					columnsToMakeNotNull.Clear();

					primaryKeyConstraint
						.ColumnNames
						.Where(c => c != autoIncrementColumn.ColumnDefinition.ColumnName)
						.ForEach(c => columnsToMakeNotNull.Add(c));

					createTableExpression = new SqlCreateTableExpression
					(
						createTableExpression.Table,
						createTableExpression.IfNotExist,
						this.VisitExpressionList(createTableExpression.ColumnDefinitionExpressions),
						newTableConstraints
					);

					columnsToMakeNotNull.Clear();

					return createTableExpression;
				}
			}

			return base.VisitCreateTable(createTableExpression);
		}

		protected override Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			var autoIncrement = columnDefinitionExpression.ConstraintExpressions
				.OfType<SqlSimpleConstraintExpression>()
				.Any(c => c.Constraint == SqlSimpleConstraint.AutoIncrement);

			IEnumerable<Expression> newConstraints = columnDefinitionExpression.ConstraintExpressions;

			if (columnsToMakeNotNull.Contains(columnDefinitionExpression.ColumnName))
			{
				newConstraints = newConstraints
					.Concat(new SqlSimpleConstraintExpression(SqlSimpleConstraint.NotNull));
			}

			if (autoIncrement)
			{
				newConstraints = newConstraints
					.Where(c => !(c is SqlSimpleConstraintExpression) || ((SqlSimpleConstraintExpression)c).Constraint != SqlSimpleConstraint.NotNull)
					.Prepend(new SqlSimpleConstraintExpression(SqlSimpleConstraint.PrimaryKey));
			}

			if (object.ReferenceEquals(newConstraints, columnDefinitionExpression.ConstraintExpressions))
			{
				return base.VisitColumnDefinition(columnDefinitionExpression);
			}
			else
			{
				return columnDefinitionExpression.UpdateConstraints(newConstraints);
			}
		}
	}
}
