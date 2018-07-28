// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Tests.OtherDataAccessObjects
{
	[DataAccessObject(NotPersisted = true)]
	public abstract class Fruit
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract string Color { get; set; }
	}
}
