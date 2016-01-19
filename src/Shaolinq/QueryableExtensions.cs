// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq
{
	public static class QueryableExtensions
	{
		public static T EachItem<T>(this IQueryable<T> source)
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
	}
}
