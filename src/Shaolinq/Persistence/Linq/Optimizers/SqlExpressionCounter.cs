// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlExpressionCounter
		: SqlExpressionVisitor
	{
		private int count;
		private readonly Predicate<Expression> isMatch;
		
		private SqlExpressionCounter(Predicate<Expression> isMatch)
		{
			this.isMatch = isMatch;
		}

		public static int Count(Expression expression, Predicate<Expression> isMatch)
		{
			var finder = new SqlExpressionCounter(isMatch);

			finder.Visit(expression);

			return finder.count;
		}

		protected override Expression Visit(Expression expression)
		{
			if (expression == null)
			{
				return null;
			}

			if (this.isMatch(expression))
			{
				this.count++;
			}

			return base.Visit(expression);
		}
	}
}
