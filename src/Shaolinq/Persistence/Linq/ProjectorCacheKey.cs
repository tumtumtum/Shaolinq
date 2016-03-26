// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	internal struct ProjectorCacheKey
	{
		internal readonly int hashCode;
		internal readonly LambdaExpression projectionExpression;

		public ProjectorCacheKey(LambdaExpression projectionExpression)
		{
			this.projectionExpression = projectionExpression;
			this.hashCode = SqlExpressionHasher.Hash(this.projectionExpression, SqlExpressionComparerOptions.IgnoreConstantPlaceholders);
		}
	}
}