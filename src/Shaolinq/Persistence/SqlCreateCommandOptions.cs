// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence
{
	[Flags]
	public enum SqlCreateCommandOptions
	{
		None = 0,
		UnpreparedExecute = 1,
		Default = None
	}
}
