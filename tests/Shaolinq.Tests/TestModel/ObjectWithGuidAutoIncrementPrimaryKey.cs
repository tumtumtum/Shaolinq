// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithGuidAutoIncrementPrimaryKey
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
