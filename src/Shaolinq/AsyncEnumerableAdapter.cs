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
            return getEnumerator();
        }

	    public IEnumerator<T> GetEnumerator()
	    {
		    return getEnumerator();
	    }

	    IEnumerator IEnumerable.GetEnumerator()
	    {
		    return this.GetEnumerator();
	    }
    }
}
