namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithRelatedObject
		: DataAccessObject<long>
	{
		[AutoIncrement(false)]
		public abstract override long Id { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		public abstract ObjectWithRelatedObject RelatedObject { get; set; }
	}
}