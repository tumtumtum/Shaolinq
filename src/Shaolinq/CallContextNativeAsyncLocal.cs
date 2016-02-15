using System;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace Shaolinq
{
	internal class CallContextNativeAsyncLocal<T>
		: AsyncLocal<T>
	{
		private readonly string key;

		internal class Container
			: MarshalByRefObject
		{
			public Container(T value)
			{
				this.Value = value;
			}

			public T Value { get; set; }
		}

		internal class Counter
		{
			internal static long count = 0;
		}
		
		public override T Value
		{
			get
			{
				var container = (Container)CallContext.LogicalGetData(this.key);

				return container != null ? container.Value : default(T);
			}
			set { CallContext.LogicalSetData(this.key, new Container(value)); }
		}

		public CallContextNativeAsyncLocal()
			: base(null)
		{
			Interlocked.Increment(ref Counter.count);

			this.key = "CallContextNativeAsyncLocal#" + Counter.count;
		}

		public override void Dispose()
		{
			CallContext.FreeNamedDataSlot(this.key);
		}
	}
}