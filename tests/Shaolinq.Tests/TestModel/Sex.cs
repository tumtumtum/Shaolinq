// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[SizeConstraint(MaximumLength = 8)]
	public enum Sex : short
	{
		Male,
		Female
	}
}
