using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq
{
	internal class AsyncEnumeratorAdapter<T>
		: IAsyncEnumerator<T>
	{
		private readonly IEnumerator<T> enumerator;

		public T Current => this.enumerator.Current;
		public void Dispose() => this.enumerator.Dispose();

		public AsyncEnumeratorAdapter(IEnumerator<T> enumerator)
		{
			this.enumerator = enumerator;
		}

		public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(this.enumerator.MoveNext());
		}
	}
}