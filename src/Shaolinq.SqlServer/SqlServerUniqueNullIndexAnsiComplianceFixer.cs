// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerUniqueNullIndexAnsiComplianceFixer
		: SqlExpressionVisitor
	{
		private class SqlColumnComparisonFinder
			: SqlExpressionVisitor
		{
			private HashSet<string> columnNames;

			public static HashSet<string> Find(Expression expression)
			{
				var finder = new SqlColumnComparisonFinder();

				finder.Visit(expression);

				return finder.columnNames;
			}

			protected override Expression VisitBinary(BinaryExpression binaryExpression)
			{
				if (binaryExpression.Left is SqlColumnExpression column1)
				{
					(this.columnNames ?? (this.columnNames = new HashSet<string>())).Add(column1.Name);
				}
				else if (binaryExpression.Right is SqlColumnExpression column2)
				{
					(this.columnNames ?? (this.columnNames = new HashSet<string>())).Add(column2.Name);
				}

				return base.VisitBinary(binaryExpression);
			}
		}

		private readonly bool fixNonUniqueIndexesAsWell;
		private readonly bool explicitIndexConditionOverridesNullAnsiCompliance;

		private SqlServerUniqueNullIndexAnsiComplianceFixer(bool fixNonUniqueIndexesAsWell, bool explicitIndexConditionOverridesNullAnsiCompliance)
		{
			this.fixNonUniqueIndexesAsWell = fixNonUniqueIndexesAsWell;
			this.explicitIndexConditionOverridesNullAnsiCompliance = explicitIndexConditionOverridesNullAnsiCompliance;
		}

		public static Expression Fix(Expression expression, bool fixNonUniqueIndexesAsWell = false, bool explicitIndexConditionOverridesNullAnsiCompliance = false)
		{
			return new SqlServerUniqueNullIndexAnsiComplianceFixer(fixNonUniqueIndexesAsWell, explicitIndexConditionOverridesNullAnsiCompliance).Visit(expression);
		}

		protected override Expression VisitCreateIndex(SqlCreateIndexExpression createIndexExpression)
		{
			if (createIndexExpression.Where != null && this.explicitIndexConditionOverridesNullAnsiCompliance)
			{
				return createIndexExpression;
			}

		    if (!(createIndexExpression.Unique || this.fixNonUniqueIndexesAsWell))
		    {
		        return createIndexExpression;
		    }

			var referencedColumns = createIndexExpression.Where == null ? null : SqlColumnComparisonFinder.Find(createIndexExpression.Where);

			var predicate = createIndexExpression
				.Columns
				.Where(c => referencedColumns == null || !referencedColumns.Contains(c.Column.Name))
				.Select(c => (Expression)new SqlFunctionCallExpression(typeof(bool), SqlFunction.IsNotNull, c.Column))
				.Aggregate(Expression.And);

			return createIndexExpression.ChangeWhere(createIndexExpression.Where == null ? predicate : Expression.And(createIndexExpression.Where, predicate));
		}
	}
}
