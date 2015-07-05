using System;

namespace Shaolinq.Tests.OtherDataAccessObjects
{
	[DataAccessObject(NotPersisted = true)]
	public abstract class Fruit
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract string Color { get; set; }
	}
}
