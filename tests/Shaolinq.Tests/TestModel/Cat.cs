namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Cat
		: DataAccessObject<long>
	{
		[Index(IndexName = "Index")]
		[PersistedMember]
		public abstract string Name { get; set; }

		[Index(IndexName = "Index")]
		[PersistedMember]
		public abstract int LivesRemaining { get; set; }

		[PersistedMember]
		public abstract Dog Companion { get; set; }
	}
}
