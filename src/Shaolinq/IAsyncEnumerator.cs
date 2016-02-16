// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq
{
	public interface IAsyncEnumerator<out T>
		: IEnumerator<T>
	{
		Task<bool> MoveNextAsync(CancellationToken cancellationToken);
	}
}
