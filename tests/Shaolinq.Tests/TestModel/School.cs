// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class School
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		[ComputedTextMember("urn:$(PERSISTED_TYPENAME:L):{Id}")]
		public abstract string Urn { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Student> Students { get; }

		[PersistedMember]
		public abstract Address Address { get; set; }
	}
}
