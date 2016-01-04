using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlOuterQueryReferencePlaceholderSubstitutor
		: SqlExpressionVisitor
	{
		private int startIndex;
		private readonly string outerAlias;

		private SqlOuterQueryReferencePlaceholderSubstitutor(int startIndex, string outerAlias)
		{
			this.startIndex = startIndex;
			this.outerAlias = outerAlias;
		}

		public static Expression Substitute(Expression expression, string outerAlias, int startIndex)
		{
			return new SqlOuterQueryReferencePlaceholderSubstitutor(startIndex, outerAlias).Visit(expression);
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (columnExpression.SelectAlias == outerAlias)
			{
				return new SqlConstantPlaceholderExpression(startIndex++, null);
			}

			return base.VisitColumn(columnExpression);
		}
	}
}
