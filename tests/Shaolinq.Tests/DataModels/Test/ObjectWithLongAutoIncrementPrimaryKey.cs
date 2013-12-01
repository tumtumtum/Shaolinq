// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Tests.DataModels.Test
{
	[DataAccessObject]
	public abstract class ObjectWithLongAutoIncrementPrimaryKey
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
