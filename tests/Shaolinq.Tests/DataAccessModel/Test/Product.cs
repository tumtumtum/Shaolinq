using System;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class Product
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract double Price { get; set; }
	}
}
