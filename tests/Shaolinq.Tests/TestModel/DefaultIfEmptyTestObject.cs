// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class DefaultIfEmptyTestObject
		: DataAccessObject<int>
	{
		[PersistedMember]
		public abstract string Text { get; set; }

		[PersistedMember]
		[DefaultValue(0)]
		public abstract int Integer { get; set; }

		[PersistedMember]
		public abstract int? NullableInteger { get; set; }
	}
}
