namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class A : DataAccessObject<long>
	{
		[PersistedMember]
		public virtual string Data { get; set; }
	}
}
