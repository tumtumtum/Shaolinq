// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence.Linq
{
	[Flags]
	public enum SqlQueryFormatterOptions
	{
		None,
		EvaluateConstantPlaceholders = 1,
		ExpectSchemaExpressions = 2,
		OptimiseOutConstantNulls = 4,
		EvaluateConstants = 8,
		Default = EvaluateConstantPlaceholders | ExpectSchemaExpressions | OptimiseOutConstantNulls
	}
}
