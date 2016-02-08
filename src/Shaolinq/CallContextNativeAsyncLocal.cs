using System;
using System.Runtime.Remoting.Messaging;

namespace Shaolinq
{
	internal class CallContextNativeAsyncLocal<T>
		: AsyncLocal<T>
	{
		private readonly string key = Guid.NewGuid().ToString("N");

		internal class Container
			: MarshalByRefObject
		{
			public Container(T value)
			{
				this.Value = value;
			}

			public T Value { get; set; }
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
			: base(true)
		{	
		}

		public override void Dispose()
		{
			CallContext.FreeNamedDataSlot(this.key);
		}
	}
}