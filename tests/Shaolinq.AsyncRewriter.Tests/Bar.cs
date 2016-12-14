using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform;

namespace Shaolinq.AsyncRewriter.Tests.NS1
{
	public static class Extensions
	{
		public static async IEnumerable<string> SingleAsync(this IEnumerable<string> s)
		{
			return new string[0];
		}
	}

	public partial class Bar: Foo
	{
		[RewriteAsync]
		public override void Method1()
		{
			Method2("hello", null);
		}

		[RewriteAsync]
		public override void Method2(string text, System.Collections.Generic.IReadOnlyList<Platform.Pair<string>> names)
		{	
		}

		[RewriteAsync]
		public override void Method3<T>()
		{
		}

		[RewriteAsync]
		public List<string> GetAll()
		{
			
		}

		public async void Test()
		{
			var dao = await (GetAll()).SingleAsync();
			var dao2 = await (await GetAllAsync().ConfigureAwait(false)).SingleAsync();
		}
	}
}
