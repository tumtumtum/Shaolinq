// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlAggregateProjectionNormalizer
		: SqlExpressionVisitor
	{
		public static Expression Normalize(Expression expression)
		{
			return new SqlAggregateProjectionNormalizer().Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			if (selectExpression.OrderBy != null)
			{
				return selectExpression.ChangeOrderBy(null);
			}

			return base.VisitSelect(selectExpression);
		}
	}
}
