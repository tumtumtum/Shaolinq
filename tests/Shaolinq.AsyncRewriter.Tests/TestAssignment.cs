// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	public interface IAsyncEnumerator<T>: IEnumerator<T>
	{
		Task<bool> MoveNextAsync();
	}

	public class Array<T>
	{
		public IEnumerator<T> GetEnumerator()
		{
			return null;
		}

		public Task<IAsyncEnumerator<T>> GetEnumeratorAsync()
		{
			return null;
		}
	}

	public class Program
	{
		[RewriteAsync]
		public void Test()
		{
			using (var enumerator = new Array<int>().GetEnumerator())
			{
				enumerator.MoveNext();
			}
		}
	}
}
