// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithGuidNonAutoIncrementPrimaryKey
		: DataAccessObject<Guid>
	{
		[AutoIncrement(false)]
		public abstract override Guid Id { get; set; }
	}
}
