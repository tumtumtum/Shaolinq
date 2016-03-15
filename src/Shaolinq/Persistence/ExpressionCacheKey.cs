// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence
{
	internal struct ExpressionCacheKey
	{
		public int hash;
		public Expression expression;
		
		public ExpressionCacheKey(Expression expression)
			: this()
		{
			this.expression = expression;
			this.hash = SqlExpressionHasher.Hash(expression, SqlExpressionComparerOptions.IgnoreConstantPlaceholders);
		}
	}
}