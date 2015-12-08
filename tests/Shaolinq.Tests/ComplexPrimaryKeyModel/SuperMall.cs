namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class SuperMall
		: DataAccessObject<Mall>
	{
		[PrimaryKey]
		[PersistedMember]
		public abstract Address Address1 { get; set; }

		[PrimaryKey]
		[PersistedMember]
		public abstract Address Address2 { get; set; }
	}
}