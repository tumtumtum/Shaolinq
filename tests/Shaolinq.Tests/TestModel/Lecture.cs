// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Lecture
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract Paper Paper { get; set; }
	}
}
