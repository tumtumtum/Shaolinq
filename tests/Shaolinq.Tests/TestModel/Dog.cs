using System;
using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Dog
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Name { get; set; }

		[Unique]
		[AutoIncrement]
		[PersistedMember]
		public abstract long SerialNumber { get; set; }

		[AutoIncrement]
		[PersistedMember]
		public abstract Guid FavouriteGuid { get; set; }

		[PersistedMember]
		public abstract Cat CompanionCat { get; set; }
	}
}
