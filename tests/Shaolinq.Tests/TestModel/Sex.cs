// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[SizeConstraint(MaximumLength = 18)]
	public enum Sex : short
	{
		Male,
		Female
	}
}
