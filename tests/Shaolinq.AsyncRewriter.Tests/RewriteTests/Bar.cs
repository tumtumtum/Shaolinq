using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests.NS1
{
	public static class Extensions
	{
		public static async Task<IEnumerable<string>> SingleAsync(this IEnumerable<string> s)
		{
			return new string[0];
		}
	}

	public partial class Bar: Foo
	{
		[RewriteAsync]
		public override void Method1()
		{
			var uuid = Guid.NewGuid();

			Method2($"hello {uuid:N}", null);

			Method2($"hello {uuid:N}", null);

			Method2($"hello {uuid:N}", null);
		}

		[RewriteAsync]
		public override void Method2(string text, System.Collections.Generic.IReadOnlyList<string> names)
		{	
		}

		[RewriteAsync]
		public override void Method3<T>()
		{
		}

		[RewriteAsync]
		public List<string> GetAll()
		{
			return null;
		}

		public async Task Test()
		{
			var dao = await (GetAll()).SingleAsync();
			var dao2 = await (await GetAllAsync().ConfigureAwait(false)).SingleAsync();
		}

		[RewriteAsync]
		public override void AbstractMethod1()
		{
		}
	}
}
