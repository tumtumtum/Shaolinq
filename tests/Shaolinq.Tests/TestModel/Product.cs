// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;

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

		[PersistedMember, DefaultValue("00:00")]
		public abstract TimeSpan ShelfLife { get; set; }
	}
}
