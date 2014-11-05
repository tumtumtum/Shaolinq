// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq
{
	public static class QueryableExtensions
	{
		public static IQueryable<T> WhereForUpdate<T>(this DataAccessObjectsQueryable<T> queryable, Expression<Func<T, bool>> condition)
			where T : DataAccessObject
		{
			return queryable.PersistenceQueryProvider.CreateQuery<T>(Expression.Call(null,  ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), Expression.Constant(queryable), condition));
		}

		public static IQueryable<R> SelectForUpdate<T, R>(this DataAccessObjectsQueryable<T> queryable, Expression<Func<T, R>> condition)
			where T : DataAccessObject
		{
			return queryable.PersistenceQueryProvider.CreateQuery<R>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), Expression.Constant(queryable), condition));
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
	}
}
