// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Bird
		: DataAccessObject
	{	
		[PrimaryKey]
		[AutoIncrement]
		[PersistedMember]
		public abstract Cat Owner { get; set; }
	}
}