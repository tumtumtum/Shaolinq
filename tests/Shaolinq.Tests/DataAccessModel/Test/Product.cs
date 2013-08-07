using System;

namespace Shaolinq.Tests.DataAccessModel.Test
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
