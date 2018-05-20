// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithLongNonAutoIncrementPrimaryKey
		: DataAccessObject<long>
	{
		[AutoIncrement(false)]
		public abstract override long Id { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
