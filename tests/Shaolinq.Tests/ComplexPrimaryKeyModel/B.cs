namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class B : DataAccessObject<A>
	{
		[PersistedMember(Name = "Id", PrefixName = "")]
		public abstract override A Id { get;set; }

		[PersistedMember]
		public abstract string MoreData { get; set; }
	}
}
