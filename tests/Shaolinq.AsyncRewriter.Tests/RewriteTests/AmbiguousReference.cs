using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Other
{
	public static partial class QueryableExtensions
	{
		public static Task<T> SingleAsync<T>(this IQueryable<T> queryable)
		{
			return null;
		}

		public static Task<T> SingleAsync<T>(this IQueryable<T> queryable, CancellationToken cancellationToken)
		{
			return null;
		}
	}
}

namespace Shaolinq.AsyncRewriter.Tests.RewriteTests
{
	public partial class AmbiguousReference
	{
		[RewriteAsync]
		public void Test(IQueryable<object> queryable)
		{
			var value = queryable.Where(c => true).Select(c => c).Single();
		} 
	}
}
