using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	public static class Extensions
	{
		public static async Task<List<T>> ToListAsync<T>(this IEnumerable<T> list)
		{
			return null;
		}
	}

	public partial class TestExplicitAwaitRewrittenAsyncMethod
	{
		[RewriteAsync]
		public IQueryable<string> GetAll()
		{
			return null;
		}

		public async Task<bool> Test()
		{
			var query = await GetAllAsync();

			var accounts = await query.Where(x => true).ToListAsync();

			return true;
		}
	}
}
