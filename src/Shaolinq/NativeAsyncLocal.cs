// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	internal class NativeAsyncLocal<T>
		: AsyncLocal<T>
	{
		public static bool Supported => asyncLocalType != null;
		private static readonly Type asyncLocalType = Type.GetType("System.Threading.AsyncLocal`1")?.MakeGenericType(typeof(T));

		private readonly dynamic nativeAsyncLocal;

		public NativeAsyncLocal()
		{
			this.nativeAsyncLocal = Activator.CreateInstance(asyncLocalType);
		}

		public override T Value { get { return this.nativeAsyncLocal.Value; } set { this.nativeAsyncLocal.Value = value; } }
	}
}