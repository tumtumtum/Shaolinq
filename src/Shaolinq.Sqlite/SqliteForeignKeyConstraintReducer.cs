using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Sqlite
{
	public class SqliteForeignKeyConstraintReducer
		: SqlExpressionVisitor
	{
		private readonly IDictionary<string, string> primaryKeyNameByTablesWithReducedPrimaryKeyName = new Dictionary<string, string>();
		private SqlCreateTableExpression currentCreateTableExpression;

		private SqliteForeignKeyConstraintReducer(IDictionary<string, string> primaryKeyNameByTablesWithReducedPrimaryKeyName)
		{
			this.primaryKeyNameByTablesWithReducedPrimaryKeyName = primaryKeyNameByTablesWithReducedPrimaryKeyName;
		}

		public static Expression Reduce(Expression expression, IDictionary<string, string> primaryKeyNameByTablesWithReducedPrimaryKeyName)
		{
			return new SqliteForeignKeyConstraintReducer(primaryKeyNameByTablesWithReducedPrimaryKeyName).Visit(expression);
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var previousCreateTableExpression = this.currentCreateTableExpression;

			this.currentCreateTableExpression = createTableExpression;

			var retval = base.VisitCreateTable(createTableExpression);

			this.currentCreateTableExpression = previousCreateTableExpression;

			return retval;
		}

		protected override Expression VisitForeignKeyConstraint(SqlForeignKeyConstraintExpression foreignKeyConstraintExpression)
		{
			string primaryKeyName;

			if (primaryKeyNameByTablesWithReducedPrimaryKeyName.TryGetValue(foreignKeyConstraintExpression.ReferencesColumnExpression.ReferencedTableName, out primaryKeyName))
			{
				var index = foreignKeyConstraintExpression.ReferencesColumnExpression.ReferencedColumnNames.IndexOf(primaryKeyName);

				var newColumnNames = foreignKeyConstraintExpression.ColumnNames.Where((c, i) => i == index);
				var newReferencedColumnNames = foreignKeyConstraintExpression.ReferencesColumnExpression.ReferencedColumnNames.Where((c, i) => i == index);

				return foreignKeyConstraintExpression.UpdateColumnNamesAndReferencedColumnExpression(newColumnNames, foreignKeyConstraintExpression.ReferencesColumnExpression.UpdateReferencedColumnNames(newReferencedColumnNames));
			}
			
			return base.VisitForeignKeyConstraint(foreignKeyConstraintExpression);
		}
	}
}
