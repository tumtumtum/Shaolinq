using System;
using System.Reflection;

namespace Shaolinq
{
	internal class AsyncLocal<T>
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
	}

	internal class NativeAsyncLocal<T>
		: AsyncLocal<T>
	{
		public static bool Supported => asyncLocalType != null;
		private static readonly Type asyncLocalType = Type.GetType("System.Threading.AsyncLocal`1")?.MakeGenericType(typeof(T));

		private readonly dynamic nativeAsyncLocal;

		public NativeAsyncLocal()
		{
			nativeAsyncLocal = Activator.CreateInstance(asyncLocalType);
		}

		public override T Value { get { return nativeAsyncLocal.Value; } set { nativeAsyncLocal.Value = value; } }
	}

	internal class CallContextNativeAsyncLocal<T>
		: AsyncLocal<T>
	{
		private readonly string key = Guid.NewGuid().ToString("N");

		public static bool Supported => callContextType != null;
		private static readonly Type callContextType = Type.GetType("System.Runtime.Remoting.Messaging.CallContext");

		public override T Value { get { return (T)callContextType.InvokeMember("LogicalGetData", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null, new object[] { key }); } set { callContextType.InvokeMember("LogicalSetData", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null, new object[] { key, value }); } }
	}
}
