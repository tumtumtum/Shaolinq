using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	namespace Fab
	{
		public static class F
		{
			public static Task<int> FooAsync<T>(this List<T> foo, string s, CancellationToken cancellationToken)
			{
				return Task.FromResult(0);
			}
		}
	}

	public abstract class Foo
	{
		public virtual void Method1()
		{	
		}

		public abstract void Method2(string text, IReadOnlyList<string> names);
		public abstract Task Method2Async(string text, System.Collections.Generic.IReadOnlyList<string> names);
		public abstract Task Method2Async(string text, System.Collections.Generic.IReadOnlyList<string> names, CancellationToken cancellationToken);
		
		public virtual Task Method1Async()
		{
			return null;
		}

		public virtual void Method3<T>()
			where T : class
		{	
		}

		[RewriteAsync]
		public abstract void AbstractMethod1();
	}
}
