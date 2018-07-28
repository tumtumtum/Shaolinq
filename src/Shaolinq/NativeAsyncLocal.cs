// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq
{
	internal class NativeAsyncLocal<T>
		: AsyncLocal<T>
	{
		private static readonly Type NativeAsyncLocalType;
		public static bool Supported => NativeAsyncLocalType != null;
		private static readonly Func<object, T> getValueFunc;
		private static readonly Action<object, T> setValueFunc;
		private static readonly Func<object> createAsyncLocalFunc;
		
		internal static bool IsRunningMono()
		{
			return Type.GetType("Mono.Runtime") != null;
		}

		static NativeAsyncLocal()
		{
			NativeAsyncLocalType = IsRunningMono() ? null : Type.GetType("System.Threading.AsyncLocal`1")?.MakeGenericType(typeof(T));

			if (NativeAsyncLocalType != null)
			{
				var param1 = Expression.Parameter(typeof(object));
				var param2 = Expression.Parameter(typeof(T));
				
				getValueFunc = Expression.Lambda<Func<object, T>>(Expression.Property(Expression.Convert(param1, NativeAsyncLocalType), "Value"), param1).Compile();
				setValueFunc = Expression.Lambda<Action<object, T>>(Expression.Assign(Expression.Property(Expression.Convert(param1, NativeAsyncLocalType), "Value"), param2), param1, param2).Compile();

				createAsyncLocalFunc = Expression.Lambda<Func<object>>(Expression.New(NativeAsyncLocalType)).Compile();
			}
		}

		private readonly object nativeAsyncLocal;

		public NativeAsyncLocal()
			: base(null)
		{
			this.nativeAsyncLocal = createAsyncLocalFunc();
		}

		public override T Value { get { return getValueFunc(this.nativeAsyncLocal); } set { setValueFunc(this.nativeAsyncLocal, value); } }
	}
}