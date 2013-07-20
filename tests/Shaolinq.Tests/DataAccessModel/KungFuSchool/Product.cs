using System;

namespace Shaolinq.Tests.DataAccessModel.KungFuSchool
{
	[DataAccessObject]
	public abstract class Product
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract double Price { get; set; }
	}
}
