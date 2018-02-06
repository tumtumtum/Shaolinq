using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Shaolinq
{
	// Used by AmbiguousReference.cs test
	public static class QueryableExtensions
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
