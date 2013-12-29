// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence.Linq
{
	[Flags]
	public enum SqlQueryFormatterOptions
	{
		None,
		EvaluateConstantPlaceholders = 1,
		ExpectSchemaExpressions = 2,
		Default = EvaluateConstantPlaceholders | ExpectSchemaExpressions
	}
}
