// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Product
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		public abstract double Price { get; set; }

		[PersistedMember]
		public abstract TimeSpan ShelfLife { get; set; }
	}
}
