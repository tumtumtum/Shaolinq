// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace Shaolinq
{
	internal class CallContextNativeAsyncLocal
	{
		internal static long count = 0;
	}
	
	internal class CallContextNativeAsyncLocal<T>
		: AsyncLocal<T>
	{
		private readonly string key;

		public override T Value
		{
			get
			{
				var container = CallContext.LogicalGetData(this.key) as ByRefContainer<T>;

				return container == null ? default(T) : container.value;
			}
			set { CallContext.LogicalSetData(this.key, new ByRefContainer<T>(value)); }
		}

		public CallContextNativeAsyncLocal()
			: base(null)
		{            
			var id = Interlocked.Increment(ref CallContextNativeAsyncLocal.count);

			this.key = "SLQ-CCNAL#" + id;
		}

		public override void Dispose()
		{
			CallContext.FreeNamedDataSlot(this.key);
		}
	}
}