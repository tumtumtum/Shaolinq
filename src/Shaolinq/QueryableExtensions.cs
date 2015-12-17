// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Shaolinq
{
	public static class QueryableExtensions
	{
		public static IQueryable<T> WhereForUpdate<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> condition)
			where T : DataAccessObject
		{
			return queryable.Provider.CreateQuery<T>(Expression.Call(null,  ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), Expression.Constant(queryable), condition));
		}

		public static IQueryable<R> SelectForUpdate<T, R>(this IQueryable<T> queryable, Expression<Func<T, R>> condition)
			where T : DataAccessObject
		{
			return queryable.Provider.CreateQuery<R>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), Expression.Constant(queryable), condition));
		}

		public static IQueryable<T> Include<T, U>(this IQueryable<T> source, Expression<Func<T, U>> include)
			where U : DataAccessObject
		{
			return source.Provider.CreateQuery<T>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new [] {typeof(T), typeof(U)}), new []
			{
				source.Expression,
				Expression.Quote(include)
			}));
		}

		public static Task<List<T>> ToListAsync<T>()
		{
			throw new NotImplementedException();
		}
	}
}
