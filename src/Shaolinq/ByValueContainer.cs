// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq
{
	internal struct ByValueContainer<T>
	{
		public T value;

		public ByValueContainer(T value)
		{
			this.value = value;
		}
	}
}