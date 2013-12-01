// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Tests.DataModels.Test
{
	[DataAccessObject]
	public abstract class Lecture
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract Paper Paper { get; set; }
	}
}
