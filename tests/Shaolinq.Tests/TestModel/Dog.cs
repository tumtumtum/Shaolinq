namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Dog
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		public abstract Cat CompanionCat { get; set; }
	}
}
