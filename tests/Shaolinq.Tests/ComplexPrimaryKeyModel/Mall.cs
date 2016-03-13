// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public class Mall
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public virtual string Name { get; set; }

		[PersistedMember]
		public virtual Address Address { get; set; }

		[PersistedMember]
		public virtual Mall SisterMall { get; set; }

		[PersistedMember]
		public virtual Mall SisterMall2 { get; set; }

        [PersistedMember]
        public virtual Shop TopShop { get; set; }

        [RelatedDataAccessObjects]
		public virtual RelatedDataAccessObjects<Shop> Shops { get; }

		[RelatedDataAccessObjects(BackReferenceName = "Mall2")]
		public virtual RelatedDataAccessObjects<Shop> Shops2 { get; }

		[RelatedDataAccessObjects(BackReferenceName = "Mall3")]
		public virtual RelatedDataAccessObjects<Shop> Shops3 { get; }
	}
}
