using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class SqlExpressionPlatformDifferencesNormalizer
		: SqlExpressionVisitor
	{
		public static Expression Normalize(Expression expression)
		{
			return new SqlExpressionPlatformDifferencesNormalizer().Visit(expression);
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			// Mono Queryable.Join wraps inner in a constant even if it is IQueryable

			if (QueryBinder.IsQuery(constantExpression))
			{
				if (((IQueryable)constantExpression.Value).Expression != constantExpression)
				{
					return this.Visit(((IQueryable)constantExpression.Value).Expression);
				}
			}

			return base.VisitConstant(constantExpression);
		}
	}
}
