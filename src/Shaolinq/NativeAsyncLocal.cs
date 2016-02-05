// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq
{
	internal class NativeAsyncLocal<T>
		: AsyncLocal<T>
	{
		public static bool Supported => AsyncLocalType != null;
		private static readonly Type AsyncLocalType = Type.GetType("System.Threading.AsyncLocal`1")?.MakeGenericType(typeof(T));
	
		private readonly object nativeAsyncLocal;
		private readonly Func<object, T> getValueFunc;
		private readonly Action<object, T> setValueFunc;
		
		public NativeAsyncLocal()
		{
			this.nativeAsyncLocal = Activator.CreateInstance(AsyncLocalType);

			var param1 = Expression.Parameter(typeof(object));
			var param2 = Expression.Parameter(typeof(T));

			getValueFunc = Expression.Lambda<Func<object, T>>(Expression.Property(Expression.Convert(param1, AsyncLocalType), "Value"), param1).Compile();
			setValueFunc = Expression.Lambda<Action<object, T>>(Expression.Assign(Expression.Property(Expression.Convert(param1, AsyncLocalType), "Value"), param2), param1, param2).Compile();
		}

		public override T Value { get { return getValueFunc(this.nativeAsyncLocal); } set { setValueFunc(this.nativeAsyncLocal, value); } }
	}
}