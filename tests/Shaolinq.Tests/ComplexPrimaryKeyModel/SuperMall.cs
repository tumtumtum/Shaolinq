namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public class SuperMall
		: DataAccessObject<Mall>
	{
		[PrimaryKey]
		[PersistedMember]
		public virtual Address Address1 { get; set; }

		[PrimaryKey]
		[PersistedMember]
		public virtual Address Address2 { get; set; }
	}
}