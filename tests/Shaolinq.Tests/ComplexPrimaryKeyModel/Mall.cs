// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using Shaolinq.Tests.TestModel;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public class Mall
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public virtual string Name { get; set; }

		[AutoIncrement]
		[PersistedMember]
		public virtual long LongId { get; set; }

		[PersistedMember]
		[ComputedMember("this.CreateUrn(this.LongId)")]
		public virtual string Urn { get; set; }

		public string CreateUrn(long longId)
		{
			using (var scope = new DataAccessScope(DataAccessIsolationLevel.ReadCommitted, DataAccessScopeOptions.RequiresNew, TimeSpan.FromSeconds(60)))
			{
				((ComplexPrimaryKeyDataAccessModel)this.dataAccessModel).Shops.ToList();

				scope.Complete();
			}

			return $"urn:mall:{longId}";
		}
		
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
