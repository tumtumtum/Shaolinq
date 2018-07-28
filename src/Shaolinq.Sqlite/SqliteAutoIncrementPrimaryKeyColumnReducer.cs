// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Sqlite
{
	public class SqliteAutoIncrementPrimaryKeyColumnReducer
		: SqlExpressionVisitor
	{
		private readonly HashSet<string> columnsToMakeNotNull = new HashSet<string>();
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
				.SingleOrDefault(c => c.PrimaryKey);
			
			if (primaryKeyConstraint != null)
			{
				var autoIncrementColumns = createTableExpression
					.ColumnDefinitionExpressions
					.SelectMany(c => c.ConstraintExpressions.Select(d => new { Constraint = d, ColumnDefinition = c}))
					.Where(c => c.Constraint.AutoIncrement)
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

					if (primaryKeyConstraint.ColumnNames.Count > 1
						|| autoIncrementColumn.ColumnDefinition.ConstraintExpressions.All(c => !c.PrimaryKey))
					{
						var uniqueConstraint = new SqlConstraintExpression(ConstraintType.Unique, /* TODO: name */ null, primaryKeyConstraint.ColumnNames);

						newTableConstraints = newTableConstraints.Concat(uniqueConstraint);
					}

					this.primaryKeyNameByTablesWithReducedPrimaryKeyName[createTableExpression.Table.Name] = autoIncrementColumn.ColumnDefinition.ColumnName;

					this.columnsToMakeNotNull.Clear();

					primaryKeyConstraint
						.ColumnNames
						.Where(c => c != autoIncrementColumn.ColumnDefinition.ColumnName)
						.ForEach(c => this.columnsToMakeNotNull.Add(c));

					createTableExpression = new SqlCreateTableExpression
					(
						createTableExpression.Table,
						createTableExpression.IfNotExist,
						VisitExpressionList(createTableExpression.ColumnDefinitionExpressions),
						newTableConstraints.ToReadOnlyCollection(),
						null
					);

					this.columnsToMakeNotNull.Clear();

					return createTableExpression;
				}
			}

			return base.VisitCreateTable(createTableExpression);
		}

		protected override Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			var autoIncrement = columnDefinitionExpression
				.ConstraintExpressions
				.Any(c => c.AutoIncrement);

			IEnumerable<SqlConstraintExpression> newConstraints = columnDefinitionExpression.ConstraintExpressions;

			if (this.columnsToMakeNotNull.Contains(columnDefinitionExpression.ColumnName))
			{
				newConstraints = newConstraints
					.Concat(new SqlConstraintExpression(ConstraintType.NotNull));
			}

			if (autoIncrement)
			{
				newConstraints = newConstraints
					.Where(c => !c.NotNull)
					.Prepend(new SqlConstraintExpression(ConstraintType.PrimaryKey));
			}

			if (ReferenceEquals(newConstraints, columnDefinitionExpression.ConstraintExpressions))
			{
				return base.VisitColumnDefinition(columnDefinitionExpression);
			}
			else
			{
				return columnDefinitionExpression.ChangeConstraints(newConstraints.ToReadOnlyCollection());
			}
		}
	}
}
