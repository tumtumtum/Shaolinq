// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerUniqueNullIndexAnsiComplianceFixer
		: SqlExpressionVisitor
	{
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

			var predicate = createIndexExpression
				.Columns
				.Select(c =>  (Expression)new SqlFunctionCallExpression(typeof(bool), SqlFunction.IsNotNull, c.Column))
				.Aggregate(Expression.And);

			return createIndexExpression.ChangeWhere(createIndexExpression.Where == null ? predicate : Expression.And(createIndexExpression.Where, predicate));
		}
	}
}
