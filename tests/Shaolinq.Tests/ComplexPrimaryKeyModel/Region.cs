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
		public abstract double Diameter { get; set; }

		[PersistedMember]
		public abstract Coordinate Center { get; set; }
	}
}
