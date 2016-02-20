// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlPlatformDifferencesNormalizer
		: SqlExpressionVisitor
	{
		public static Expression Normalize(Expression expression)
		{
			return new SqlPlatformDifferencesNormalizer().Visit(expression);
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (typeof(IQueryable).IsAssignableFrom(constantExpression.Type))
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
