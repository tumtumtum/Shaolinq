// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

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