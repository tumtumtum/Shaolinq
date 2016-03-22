// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence
{
	internal struct ExpressionCacheKey
	{
		public int hash;
		public Expression expression;
		public LambdaExpression projector;

		public ExpressionCacheKey(SqlProjectionExpression expression, LambdaExpression projector)
			: this()
		{
			this.expression = expression;
			this.projector = projector;
			this.hash = SqlExpressionHasher.Hash(expression, SqlExpressionComparerOptions.IgnoreConstantPlaceholders) ^ SqlExpressionHasher.Hash(projector);
		}
	}
}