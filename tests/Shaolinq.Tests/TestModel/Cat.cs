namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Cat
		: DataAccessObject<long>
	{
		[Index(IndexName = "Index")]
		[PersistedMember]
		public abstract string Name { get; set; }

		[Index("CompositeIndexOverObjectAndNonObject", CompositeOrder = 2)]
		[Index(IndexName = "Index", SortOrder = SortOrder.Descending)]
		[PersistedMember]
		public abstract int LivesRemaining { get; set; }

		[Index("CompositeIndexOverObjectAndNonObject", CompositeOrder = 1)]
		[PersistedMember]
		public abstract Dog Companion { get; set; }

		[BackReference("ParentCatFoo")]
		[Index("CompositeIndexOverObjectAndNonObject", CompositeOrder = 3)]
		public abstract Cat Parent { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Cat> Kittens { get; }
	}
}
