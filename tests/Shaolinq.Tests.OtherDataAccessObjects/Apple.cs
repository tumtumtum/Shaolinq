// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.OtherDataAccessObjects
{
	[DataAccessObject]
	public abstract class Apple
		: Fruit
	{
		[PersistedMember]
		public abstract float Quality { get; set; }
	}
}
