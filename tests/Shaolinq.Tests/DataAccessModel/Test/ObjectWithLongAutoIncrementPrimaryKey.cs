namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class ObjectWithLongAutoIncrementPrimaryKey
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
