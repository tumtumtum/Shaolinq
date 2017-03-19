// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;

namespace Shaolinq
{
	public interface IAsyncEnumerable<out T>
		: IEnumerable<T>
	{
		IAsyncEnumerator<T> GetAsyncEnumerator();
	}
}
