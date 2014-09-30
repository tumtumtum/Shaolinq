namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Address
		: DataAccessObject<long>
	{
		[PrimaryKey]
		[PersistedMember]
		public abstract Region Region { get; set; }

		[PersistedMember]
		public abstract int Number { get; set; }

		[PersistedMember]
		public abstract string Street { get; set; }
	}
}
