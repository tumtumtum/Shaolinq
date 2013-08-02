using Platform.Validation;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class Student
		: Person
	{
		[PersistedMember]
		public abstract Sex Sex { get; set; }

		[BackReference]
		public abstract School School { get; set; }

		[BackReference, ValueRequired(false)]
		public abstract Fraternity Fraternity { get; set; }

		[PersistedMember]
		public abstract Student BestFriend { get; set; }

		[PersistedMember]
		public abstract Address Address { get; set; }
	}
}
