// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Shaolinq
{
	public enum ToListCachePolicy
	{
		Default,
		CacheOnly,
		IgnoreCache
	}

	public static class QueryableExtensions
	{
		internal static T Items<T>(this IQueryable<T> source)
			where T : DataAccessObject
		{
			return source.Provider.Execute<T>(Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), source.Expression));
		}

		public static IQueryable<T> WhereForUpdate<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> condition)
			where T : DataAccessObject
		{
			return queryable.Provider.CreateQuery<T>(Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), Expression.Constant(queryable), condition));
		}

		public static IQueryable<R> SelectForUpdate<T, R>(this IQueryable<T> queryable, Expression<Func<T, R>> condition)
			where T : DataAccessObject
		{
			return queryable.Provider.CreateQuery<R>(Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), Expression.Constant(queryable), condition));
		}

		public static IQueryable<T> Include<T, U>(this IQueryable<T> source, Expression<Func<T, U>> include)
		{
			return source.Provider.CreateQuery<T>(Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T), typeof(U)), new [] { source.Expression, Expression.Quote(include) }));
		}

        public static List<T> ToList<T>(this IQueryable<T> queryable, ToListCachePolicy cachePolicy)
			where T : DataAccessObject
		{
			var related = queryable as RelatedDataAccessObjects<T>;

			return related == null ? queryable.ToList() : related.ToList(cachePolicy);
		}

		public static List<T> ToList<T>(this RelatedDataAccessObjects<T> related, ToListCachePolicy cachePolicy = ToListCachePolicy.Default)
			where T : DataAccessObject
		{
			return related.ToList(cachePolicy);
		}

		public static Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
		{
			throw new NotImplementedException();
		}
	}
}
