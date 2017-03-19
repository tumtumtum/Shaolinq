// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Lecturer
		: DataAccessObject<Guid>
	{
		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Paper> Papers { get; }
	}
}
