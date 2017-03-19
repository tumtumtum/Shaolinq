// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	internal class ByRefContainer<T>
		: MarshalByRefObject
	{
		public T value;

		public ByRefContainer(T value)
		{
			this.value = value;
		}
	}
}