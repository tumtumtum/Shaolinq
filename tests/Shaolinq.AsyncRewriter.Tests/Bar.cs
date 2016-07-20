using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform;

namespace Shaolinq.AsyncRewriter.Tests.NS1
{
	public partial class Bar: Foo
	{
		[RewriteAsync]
		public override void Method1()
		{
		}

		[RewriteAsync]
		public override void Method2(string text, System.Collections.Generic.IReadOnlyList<Platform.Pair<string>> names)
		{	
		}

		[RewriteAsync]
		public override void Method3<T>()
		{
		}
	}
}
