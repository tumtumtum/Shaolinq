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

		private SqlServerUniqueNullIndexAnsiComplianceFixer(bool fixNonUniqueIndexesAsWell)
		{
			this.fixNonUniqueIndexesAsWell = fixNonUniqueIndexesAsWell;
		}

		public static Expression Fix(Expression expression, bool fixNonUniqueIndexesAsWell = false)
		{
			return new SqlServerUniqueNullIndexAnsiComplianceFixer(fixNonUniqueIndexesAsWell).Visit(expression);
		}

		protected override Expression VisitCreateIndex(SqlCreateIndexExpression createIndexExpression)
		{
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
