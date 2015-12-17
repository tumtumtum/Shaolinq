// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq
{
	public interface IAsyncEnumerator<out T>
		: IDisposable
	{
		T Current { get; }
		Task<bool> MoveNextAsync(CancellationToken cancellationToken);
	}
}
