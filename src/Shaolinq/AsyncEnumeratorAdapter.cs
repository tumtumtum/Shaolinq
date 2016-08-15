// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq
{
	public class AsyncEnumeratorAdapter<T>
		: IAsyncEnumerator<T>
	{
		public static IAsyncEnumerator<T> Adapt(IEnumerator<T> enumerator)
		{
			return enumerator as IAsyncEnumerator<T> ?? new AsyncEnumeratorAdapter<T>(enumerator);
		}

		public static IAsyncEnumerator<T> Adapt(IEnumerable<T> enumerable)
		{
			return enumerable.GetAsyncEnumeratorOrAdapt();
		}

		private readonly IEnumerator<T> enumerator;

		public AsyncEnumeratorAdapter(IEnumerator<T> enumerator)
		{
			this.enumerator = enumerator;
		}

		public bool MoveNext()
		{
			return this.enumerator.MoveNext();
		}

		public void Reset()
		{
			throw new NotSupportedException();
		}

		object IEnumerator.Current => this.Current;
		public T Current => this.enumerator.Current;
		public void Dispose() => this.enumerator.Dispose();

		public Task<bool> MoveNextAsync()
		{
			return MoveNextAsync(CancellationToken.None);
		}

		public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(this.enumerator.MoveNext());
		}
	}
}