// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithUniqueConstraint
		: DataAccessObject<long>
	{
		[PersistedMember, Unique]
		public abstract string Name { get; set; }
	}
}
