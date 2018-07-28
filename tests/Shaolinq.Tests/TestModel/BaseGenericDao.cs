// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject(NotPersisted = true)]
	public abstract	class BaseGenericDao<T> : DataAccessObject<long>
	{
		[BackReference]
		public abstract T RelatedObject { get; set; }
	}

	[DataAccessObject]
	public abstract class ConcreteGenericDao : BaseGenericDao<School>
	{
	}
}
