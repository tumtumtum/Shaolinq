// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	internal class AsyncLocal<T>
		: IDisposable
	{
		protected AsyncLocal()
		{
		}

		public static AsyncLocal<T> Create()
		{
			if (NativeAsyncLocal<T>.Supported)
			{
				return new NativeAsyncLocal<T>();
			}

			return new CallContextNativeAsyncLocal<T>();
		}

		public virtual T Value { get; set; }

		public virtual void Dispose()
		{
		}
	}
}
