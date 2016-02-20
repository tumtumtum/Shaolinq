// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence.Linq.Expressions
{
	[Flags]
	public enum SqlExpressionComparerOptions
	{
		None,
		IgnoreConstants = 1,
		IgnoreConstantPlaceholders = 2,
		IgnoreConstantsAndConstantPlaceholders = IgnoreConstants | IgnoreConstantPlaceholders
	}
}