// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	internal class AsyncLocal<T>
		: IDisposable
	{
		private static AsyncLocal<T> Create()
		{
			if (NativeAsyncLocal<T>.Supported && false)
			{
				return new NativeAsyncLocal<T>();
			}

			return new CallContextNativeAsyncLocal<T>();
		}

		private readonly AsyncLocal<T> internalAsyncLocal;

		public AsyncLocal()
		{
			if (this.GetType() == typeof(AsyncLocal<T>))
			{
				this.internalAsyncLocal = Create();
			}
		}

		internal AsyncLocal(AsyncLocal<T> internalAsyncLocal)
		{
			this.internalAsyncLocal = internalAsyncLocal;
		}

		public virtual T Value { get { return this.internalAsyncLocal.Value; } set { this.internalAsyncLocal.Value = value; } }

		public virtual void Dispose()
		{
			this.internalAsyncLocal?.Dispose();
		}
	}
}
