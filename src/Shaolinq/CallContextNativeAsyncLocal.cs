using System;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace Shaolinq
{
    internal class CallContextValueContainer<T>
            : MarshalByRefObject
    {
        public CallContextValueContainer(T value)
        {
            this.Value = value;
        }

        public T Value { get; set; }
    }

    internal class CallContextNativeAsyncLocal<T>
		: AsyncLocal<T>
	{
        internal class Counter
        {
            internal static long count = 0;
        }

        private readonly string key;

		public override T Value
		{
			get
			{
				var container = (CallContextValueContainer<T>)CallContext.LogicalGetData(this.key);

				return container != null ? container.Value : default(T);
			}
			set { CallContext.LogicalSetData(this.key, new CallContextValueContainer<T>(value)); }
		}

		public CallContextNativeAsyncLocal()
			: base(null)
		{            
			var id = Interlocked.Increment(ref Counter.count);

		    this.key = "CallContextNativeAsyncLocal#" + id;
		}

		public override void Dispose()
		{
			CallContext.FreeNamedDataSlot(this.key);
		}
	}
}