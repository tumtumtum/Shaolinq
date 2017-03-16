// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
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

		protected override Expression VisitConstraint(SqlConstraintExpression constraintExpression)
		{
			string primaryKeyName;

			if (constraintExpression.ReferencesExpression == null || constraintExpression.ColumnNames == null)
			{
				return base.VisitConstraint(constraintExpression);
			}
			
			if (this.primaryKeyNameByTablesWithReducedPrimaryKeyName.TryGetValue(constraintExpression.ReferencesExpression.ReferencedTable.Name, out primaryKeyName))
			{
				var index = constraintExpression.ReferencesExpression.ReferencedColumnNames.IndexOf(primaryKeyName);

				var newColumnNames = constraintExpression.ColumnNames.Where((c, i) => i == index);
				var newReferencedColumnNames = constraintExpression.ReferencesExpression.ReferencedColumnNames.Where((c, i) => i == index);

				return constraintExpression
					.ChangeColumnNames(newColumnNames.ToReadOnlyCollection())
					.ChangeReferences(constraintExpression.ReferencesExpression.ChangeReferencedColumnNames(newReferencedColumnNames));
			}
			
			return base.VisitConstraint(constraintExpression);
		}
	}
}
