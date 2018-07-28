// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.TestModel
{
	public interface IIdentified<T>
	{
		T Id { get; set; }
	}
}