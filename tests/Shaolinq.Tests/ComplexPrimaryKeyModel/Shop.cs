// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;

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

		[BackReference]
		public abstract Mall Mall2 { get; set; }

		[BackReference]
		public abstract Mall Mall3 { get; set; }

		[BackReference]
		public abstract Building Building { get; set; }

		[PersistedMember, DefaultValue("0001-01-01 00:00:00")]
		public abstract DateTime OpeningDate { get; set; }

		[PersistedMember]
		public abstract DateTime? CloseDate { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }
		
		[PersistedMember, DefaultValue(ShopType.Unknown)]
		public abstract ShopType ShopType { get; set; }

		[PersistedMember]
		public abstract Address SecondAddress { get; set; }

		[PersistedMember]
		public abstract Address ThirdAddress { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Toy> Toys { get; }
	}
}
