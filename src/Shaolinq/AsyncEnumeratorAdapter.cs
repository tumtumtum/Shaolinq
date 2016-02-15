// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq
{
    public class AsyncEnumeratorAdapter<T>
        : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> enumerator;

        public AsyncEnumeratorAdapter(IEnumerator<T> enumerator)
        {
            this.enumerator = enumerator;
        }

        public T Current => this.enumerator.Current;
        public void Dispose() => this.enumerator.Dispose();

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this.enumerator.MoveNext());
        }
    }
}