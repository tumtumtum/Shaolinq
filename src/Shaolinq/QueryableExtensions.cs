// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq
{
    internal static class SqlQueryProviderExtensions
    {
        public static T ExecuteEx<T>(this IQueryProvider queryProvider, Expression expression)
        {
            return queryProvider.Execute<T>(expression);
        }

        public static Task<T> ExecuteExAsync<T>(this IQueryProvider queryProvider, Expression expression, CancellationToken cancellationToken)
        {
            var sqlQueryProvider = queryProvider as ISqlQueryProvider;

            if (sqlQueryProvider != null)
            {
                return sqlQueryProvider.ExecuteAsync<T>(expression, cancellationToken);
            }
            else
            {
                return Task.FromResult(queryProvider.Execute<T>(expression));
            }
        }
    }

    public static partial class QueryableExtensions
	{
        [RewriteAsync(true)]
        private static T Min<T>(this IQueryable<T> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Min(default(IQueryable<T>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<T>(expression);
        }

        [RewriteAsync(true)]
        private static T Max<T>(this IQueryable<T> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Max(default(IQueryable<T>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<T>(expression);
        }

        [RewriteAsync(true)]
        private static int Sum(this IQueryable<int> source)
	    {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<int>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<int>(expression);
        }

        [RewriteAsync(true)]
        private static int? Sum(this IQueryable<int?> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<int?>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<int?>(expression);
        }
        
        [RewriteAsync(true)]
        private static long Sum(this IQueryable<long> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<long>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<long>(expression);
        }

        [RewriteAsync(true)]
        private static long? Sum(this IQueryable<long?> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<long?>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<long?>(expression);
        }

        [RewriteAsync(true)]
        private static float Sum(this IQueryable<float> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<float>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<float>(expression);
        }

        [RewriteAsync(true)]
        private static float? Sum(this IQueryable<float?> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<float?>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<float?>(expression);
        }

        [RewriteAsync(true)]
        private static double Sum(this IQueryable<double> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<double>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<double>(expression);
        }

        [RewriteAsync(true)]
        private static double? Sum(this IQueryable<double?> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<double?>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<double?>(expression);
        }

        //

        [RewriteAsync(true)]
        private static int Average(this IQueryable<int> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<int>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<int>(expression);
        }

        [RewriteAsync(true)]
        private static int? Average(this IQueryable<int?> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<int?>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<int?>(expression);
        }

        [RewriteAsync(true)]
        private static long Average(this IQueryable<long> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<long>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<long>(expression);
        }

        [RewriteAsync(true)]
        private static long? Average(this IQueryable<long?> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<long?>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<long?>(expression);
        }

        [RewriteAsync(true)]
        private static float Average(this IQueryable<float> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<float>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<float>(expression);
        }

        [RewriteAsync(true)]
        private static float? Average(this IQueryable<float?> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<float?>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<float?>(expression);
        }

        [RewriteAsync(true)]
        private static double Average(this IQueryable<double> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<double>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<double>(expression);
        }

        [RewriteAsync(true)]
        private static double? Average(this IQueryable<double?> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<double?>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<double?>(expression);
        }

        [RewriteAsync(true)]
        private static int Count(this IQueryable<double?> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Count(default(IQueryable<double?>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<int>(expression);
        }

        [RewriteAsync(true)]
        private static long LongCount(this IQueryable<double?> source)
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.LongCount(default(IQueryable<double?>))), source.Expression);

            return ((IQueryProvider)source.Provider).ExecuteEx<long>(expression);
        }

        public static T IncludedItems<T>(this IQueryable<T> source)
			where T : DataAccessObject
        {
            Expression expression = Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), source.Expression);

            return source.Provider.ExecuteEx<T>(expression);
		}

		public static IQueryable<T> WhereForUpdate<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> condition)
			where T : DataAccessObject
		{
		    Expression expression = Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), Expression.Constant(queryable), condition);
            
            return queryable.Provider.CreateQuery<T>(expression);
		}

		public static IQueryable<R> SelectForUpdate<T, R>(this IQueryable<T> queryable, Expression<Func<T, R>> condition)
			where T : DataAccessObject
		{
		    Expression expression = Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), Expression.Constant(queryable), condition);

			return queryable.Provider.CreateQuery<R>(expression);
		}

		public static IQueryable<T> Include<T, U>(this IQueryable<T> source, Expression<Func<T, U>> include)
		{
		    Expression expression = Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T), typeof(U)), new[] { source.Expression, Expression.Quote(include) });

            return source.Provider.CreateQuery<T>(expression);
		}
	}
}
