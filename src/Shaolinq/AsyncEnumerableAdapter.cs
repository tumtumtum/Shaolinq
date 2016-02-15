using System;

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
            return getEnumerator();
        }
    }
}
