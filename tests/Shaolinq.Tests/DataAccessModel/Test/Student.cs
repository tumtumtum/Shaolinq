namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class Student
		: Person
	{
		[PersistedMember]
		public abstract Belt Belt { get; set; }

		[BackReference]
		public abstract School School { get; set; }

		[PersistedMember]
		public abstract Student BestFriend { get; set; }

		[PersistedMember]
		public abstract Address Address { get; set; }
	}
}
