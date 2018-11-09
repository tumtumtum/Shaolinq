namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class A : DataAccessObject<long>
	{
		//[PersistedMember(Name = "Id")]
		//public abstract override long Id { get;set; }

		[PersistedMember]
		public virtual string Data { get; set; }
	}
}
