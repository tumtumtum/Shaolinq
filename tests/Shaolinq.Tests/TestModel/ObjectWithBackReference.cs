namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithBackReference
		: DataAccessObject<long>
	{
		[AutoIncrement(false)]
		public abstract override long Id { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }

		[BackReference]
		public abstract ObjectWithBackReference RelatedObject { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<ObjectWithBackReference> ObjectWithBackReferences { get; }
	}
}