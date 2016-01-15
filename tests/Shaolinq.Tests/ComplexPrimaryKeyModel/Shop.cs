// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Shop
		: DataAccessObject<long>
	{
		[PrimaryKey]
		[PersistedMember]
		public abstract Address Address { get; set; }

		[BackReference]
		public abstract Mall Mall { get; set; }
		
		[PersistedMember]
		public abstract DateTime OpeningDate { get; set; }

		[PersistedMember]
		public abstract DateTime? CloseDate { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		public abstract ShopType ShopType { get; set; }

		[PersistedMember]
		public abstract Address SecondAddress { get; set; }

		[PersistedMember]
		public abstract Address ThirdAddress { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Toy> Toys { get; }
	}
}
