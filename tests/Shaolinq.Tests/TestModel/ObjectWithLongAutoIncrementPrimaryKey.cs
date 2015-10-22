// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithLongAutoIncrementPrimaryKey
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
