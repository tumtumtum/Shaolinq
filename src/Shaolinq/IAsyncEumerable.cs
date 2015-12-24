// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq
{
	public interface IAsyncEumerable<out T>
	{
		IAsyncEnumerator<T> GetAsyncEnumerator();
	}
}
