// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq
{
	public interface IAsyncEnumerator<out T>
		: IEnumerator<T>
	{
	    Task<bool> MoveNextAsync();
        Task<bool> MoveNextAsync(CancellationToken cancellationToken);
	}
}
