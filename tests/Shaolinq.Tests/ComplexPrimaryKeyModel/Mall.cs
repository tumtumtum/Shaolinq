// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using Platform;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	public interface IAddressed
	{
		Address Address { get; set; }
	}

	[DataAccessObject]
	public class Building
		: DataAccessObject<Guid>
	{
		[RelatedDataAccessObjects(BackReferenceName = nameof(Shop.Building))]
		public virtual RelatedDataAccessObjects<Shop> ShopsInBuilding { get; }
	}

	[DataAccessObject]
	public class Mall
		: DataAccessObject<Guid>, IAddressed, INamed
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
			if (!this.dataAccessModel.GetCurrentSqlDatabaseContext().GetType().Name.StartsWith("Sqlite"))
			{
				using (var scope = new DataAccessScope(DataAccessIsolationLevel.ReadUncommitted, DataAccessScopeOptions.RequiresNew, TimeSpan.Zero))
				{
					((ComplexPrimaryKeyDataAccessModel)this.dataAccessModel).Shops.ToList();

					scope.Complete();
				}
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

		[PersistedMember]
		public virtual Building Building { get; set; }

		[RelatedDataAccessObjects]
		public virtual RelatedDataAccessObjects<Shop> Shops { get; }

		[RelatedDataAccessObjects(BackReferenceName = nameof(Shop.Mall2))]
		public virtual RelatedDataAccessObjects<Shop> Shops2 { get; }

		[RelatedDataAccessObjects(BackReferenceName = nameof(Shop.Mall3))]
		public virtual RelatedDataAccessObjects<Shop> Shops3 { get; }
	}
}
