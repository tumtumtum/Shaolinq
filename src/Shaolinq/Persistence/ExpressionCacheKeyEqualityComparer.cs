// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence
{
	internal class ExpressionCacheKeyEqualityComparer
		: IEqualityComparer<ExpressionCacheKey>
	{
		public static readonly ExpressionCacheKeyEqualityComparer Default = new ExpressionCacheKeyEqualityComparer();

		private ExpressionCacheKeyEqualityComparer()
		{
		}

		public bool Equals(ExpressionCacheKey x, ExpressionCacheKey y)
		{
			return SqlExpressionComparer.Equals(x.expression, y.expression, SqlExpressionComparerOptions.IgnoreConstantPlaceholders);
		}

		public int GetHashCode(ExpressionCacheKey obj)
		{
			return obj.hash;
		}
	}
}