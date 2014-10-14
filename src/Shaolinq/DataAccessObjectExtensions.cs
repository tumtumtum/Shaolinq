using System;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		public static T Include<T, U>(this T queryable, Expression<Func<T, U>> include)
			where T : IDataAccessObject
		{
			return queryable;
		}

		public static IQueryable<T> Include<T, U>(this IQueryable<T> queryable, Expression<Func<T, U>> include)
			where T : IDataAccessObject
		{
			return queryable.Select(x => x.Include(include));
		}
	}
}
