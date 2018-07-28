// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

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

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<ConcreteGenericDao> ConcreteGenericDao { get; }

		[PersistedMember]
		public abstract Address Address { get; set; }
		
		[PersistedMember, DefaultValue(500)]
		public abstract decimal SalePrice { get; set; }
	}
}
