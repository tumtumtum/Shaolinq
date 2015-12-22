// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Cat
		: DataAccessObject<long>
	{
		[PersistedMember]
		[Index(IndexName = "Index")]
		public abstract string Name { get; set; }

		[PersistedMember]
		[ComputedMember("Id + 100000000", "Id = value - 100000000")]
		public abstract long? MutatedId { get; set; }

		[Index("CompositeIndexOverObjectAndNonObject", CompositeOrder = 2)]
		[Index(IndexName = "Index", SortOrder = SortOrder.Descending)]
		[PersistedMember]
		public abstract int LivesRemaining { get; set; }

		[Index("CompositeIndexOverObjectAndNonObject", CompositeOrder = 1)]
		[PersistedMember]
		public abstract Dog Companion { get; set; }

		[BackReference("ParentCatFoo")]
		[ForeignObjectConstraint(OnDeleteAction = ForeignObjectAction.Restrict, OnUpdateAction = ForeignObjectAction.Restrict)]
		[Index("CompositeIndexOverObjectAndNonObject", CompositeOrder = 3)]
		public abstract Cat Parent { get; set; }
		
		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Cat> Kittens { get; }

		[BackReference]
		public abstract Student Student { get; set; }
	}
}
