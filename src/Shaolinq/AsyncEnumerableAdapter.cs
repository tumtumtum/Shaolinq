// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Shaolinq
{
	internal class AsyncEnumerableAdapter<T>
		: IAsyncEnumerable<T>
	{
		private readonly Func<IAsyncEnumerator<T>> getEnumerator;

		public AsyncEnumerableAdapter(Func<IAsyncEnumerator<T>> getEnumerator)
		{
			this.getEnumerator = getEnumerator;
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator()
		{
			return this.getEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this.getEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
