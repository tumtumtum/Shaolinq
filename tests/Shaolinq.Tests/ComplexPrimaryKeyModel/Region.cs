namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Region
		: DataAccessObject<long>
	{
		[PrimaryKey]
		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		public abstract float Diameter { get; set; }
	}
}
