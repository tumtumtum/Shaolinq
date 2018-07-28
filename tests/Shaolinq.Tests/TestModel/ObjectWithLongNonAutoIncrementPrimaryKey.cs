// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

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
