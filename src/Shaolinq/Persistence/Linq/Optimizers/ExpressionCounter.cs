// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class ExpressionCounter
		: SqlExpressionVisitor
	{
		private int count;
		private readonly Predicate<Expression> isMatch;
		
		private ExpressionCounter(Predicate<Expression> isMatch)
		{
			this.isMatch = isMatch;
		}

		public static int Count(Expression expression, Predicate<Expression> isMatch)
		{
			var finder = new ExpressionCounter(isMatch);

			finder.Visit(expression);

			return finder.count;
		}

		protected override Expression Visit(Expression expression)
		{
			if (expression == null)
			{
				return null;
			}

			if (isMatch(expression))
			{
				count++;
			}

			return base.Visit(expression);
		}
	}
}
