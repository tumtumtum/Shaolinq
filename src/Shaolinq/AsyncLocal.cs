// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	internal class AsyncLocal<T>
		: IDisposable
	{
		private static AsyncLocal<T> Create()
		{
			if (NativeAsyncLocal<T>.Supported)
			{
				return new NativeAsyncLocal<T>();
			}

			return new CallContextNativeAsyncLocal<T>();
		}

		private readonly AsyncLocal<T> internalAsyncLocal;

		public AsyncLocal()
		{
			this.internalAsyncLocal = Create();
		}

		public virtual T Value { get { return this.internalAsyncLocal.Value; } set { this.internalAsyncLocal.Value = value; } }

		public virtual void Dispose()
		{
			this.internalAsyncLocal.Dispose();
		}
	}
}
