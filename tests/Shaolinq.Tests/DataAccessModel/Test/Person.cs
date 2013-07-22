using System;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject(Abstract = true)]
	public abstract class Person
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract string FirstName { get; set; }

		[PersistedMember]
		public abstract string LastName { get; set; }

		[PersistedMember]
		[ComputedTextMember("{FirstName} {LastName}")]
		public abstract string	FullName { get; set; }

		[PersistedMember]
		public abstract DateTime? Birthdate { get; set; }
	}
}
