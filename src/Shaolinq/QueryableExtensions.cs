using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq
{
	public static class QueryableExtensions
	{	
		private static readonly MethodInfo WhereForUpdateMethod;
		private static readonly MethodInfo SelectForUpdateMethod;

		static QueryableExtensions()
		{
			QueryableExtensions.WhereForUpdateMethod = typeof(QueryableExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "WhereForUpdate");
			QueryableExtensions.SelectForUpdateMethod = typeof(QueryableExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "SelectForUpdate");
		}

		public static IQueryable<T> WhereForUpdate<T>(this DataAccessObjectsQueryable<T> queryable, Expression<Func<T, bool>> condition)
			where T : class, IDataAccessObject
		{
			return queryable.PersistenceQueryProvider.CreateQuery<T>(Expression.Call(null, WhereForUpdateMethod.MakeGenericMethod(typeof(T)), Expression.Constant(queryable), condition));
		}

		public static IQueryable<R> SelectForUpdate<T, R>(this DataAccessObjectsQueryable<T> queryable, Expression<Func<T, R>> condition)
			where T : class, IDataAccessObject
		{
			return queryable.PersistenceQueryProvider.CreateQuery<R>(Expression.Call(null, SelectForUpdateMethod.MakeGenericMethod(typeof(T)), Expression.Constant(queryable), condition));
		}
	}
}
