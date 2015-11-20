namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithDaoPrimaryKey
		: DataAccessObject<ObjectWithManyTypes>
	{
		[PersistedMember]
		public abstract string Something { get; set; }
	}
}