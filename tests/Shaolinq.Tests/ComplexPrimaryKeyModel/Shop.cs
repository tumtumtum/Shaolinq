namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Shop
		: DataAccessObject<long>
	{
		[PrimaryKey]
		[PersistedMember]
		public abstract Address Address { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
