using System;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject(Abstract = true)]
	public abstract class Person
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract string Firstname { get; set; }

		[PersistedMember]
		public abstract string Lastname { get; set; }

		[PersistedMember]
		public abstract string Nickname { get; set; }

		[PersistedMember]
		public abstract double Height { get; set; }

		[PersistedMember]
		[ComputedTextMember("{Firstname} {Lastname}")]
		public abstract string	Fullname { get; set; }

		[PersistedMember]
		public abstract DateTime? Birthdate { get; set; }
	}
}
	