namespace Shaolinq.Tests.DataModels.Test
{
	[DataAccessObject]
	public abstract class Cat
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		public abstract Dog Companion { get; set; }
	}
}
