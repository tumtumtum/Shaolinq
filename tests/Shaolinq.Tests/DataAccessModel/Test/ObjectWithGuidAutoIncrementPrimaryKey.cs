// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class ObjectWithGuidAutoIncrementPrimaryKey
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
