using System;

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Mall
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract string Name { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Shop> Shops { get; }
	}
}
