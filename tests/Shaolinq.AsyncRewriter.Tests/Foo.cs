using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	public abstract class Foo
	{
		public virtual void Method1()
		{	
		}

		public abstract void Method2(string text, System.Collections.Generic.IReadOnlyList<Platform.Pair<string>> names);
		public abstract Task Method2Async(string text, System.Collections.Generic.IReadOnlyList<Platform.Pair<string>> names);
		public abstract Task Method2Async(string text, System.Collections.Generic.IReadOnlyList<Platform.Pair<string>> names, CancellationToken cancellationToken);
		
		public virtual Task Method1Async()
		{
			return null;
		}
	}
}
