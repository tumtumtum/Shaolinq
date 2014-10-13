using System;

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Child
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract bool Good { get; set; }

		[PersistedMember]
		public abstract string Nickname { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Toy> Toys { get; }
	}
}
