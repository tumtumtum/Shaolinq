// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq.Tests.DataModels.Test
{
	[DataAccessObject]
	public abstract class ObjectWithGuidNonAutoIncrementPrimaryKey
		: DataAccessObject<Guid>
	{
		[AutoIncrement(false)]
		public abstract override Guid Id { get; set; }
	}
}
