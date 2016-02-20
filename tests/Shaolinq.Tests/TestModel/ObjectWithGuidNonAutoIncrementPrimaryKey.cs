// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

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
