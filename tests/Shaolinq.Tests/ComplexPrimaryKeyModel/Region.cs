namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Region
		: DataAccessObject<long>
	{
		[PrimaryKey]
		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
