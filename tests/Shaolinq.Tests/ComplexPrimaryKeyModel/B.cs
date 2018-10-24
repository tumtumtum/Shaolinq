namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class B : DataAccessObject<A>
	{
		[PersistedMember]
		public abstract string MoreData { get; set; }
	}
}
