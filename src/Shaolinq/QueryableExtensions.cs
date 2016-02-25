// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.TypeBuilding;

// ReSharper disable InvokeAsExtensionMethod

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
        private static T First<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
        {
            Expression expression = Expression.Call
            (
                TypeUtils.GetMethod(() => Queryable.First<T>(default(IQueryable<T>))),
                Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
            );

            return ((IQueryProvider)source.Provider).ExecuteEx<T>(expression);
        }

        [RewriteAsync(true)]
        private static T FirstOrDefault<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
        {
            Expression expression = Expression.Call
            (
                TypeUtils.GetMethod(() => Queryable.FirstOrDefault<T>(default(IQueryable<T>))),
                Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
            );

            return ((IQueryProvider)source.Provider).ExecuteEx<T>(expression);
        }

        [RewriteAsync(true)]
        private static T Single<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
        {
            Expression expression = Expression.Call
            (
                TypeUtils.GetMethod(() => Queryable.Single<T>(default(IQueryable<T>))),
                Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
            );

            return ((IQueryProvider)source.Provider).ExecuteEx<T>(expression);
        }

        [RewriteAsync(true)]
        private static T SingleOrDefault<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
        {
            Expression expression = Expression.Call
            (
                TypeUtils.GetMethod(() => Queryable.SingleOrDefault<T>(default(IQueryable<T>))),
                Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
            );

            return ((IQueryProvider)source.Provider).ExecuteEx<T>(expression);
        }

        [RewriteAsync]
        public static int Delete<T>(this IQueryable<T> source)
            where T : DataAccessObject
        {
            Expression expression = Expression.Call(TypeUtils.GetMethod(() => QueryableExtensions.Delete<T>(default(IQueryable<T>))), source.Expression);

			return ((IQueryProvider)source.Provider).ExecuteEx<int>(expression);
        }

		[RewriteAsync]
		public static int Delete<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
			where T : DataAccessObject
		{
		    Expression expression = Expression.Call
            (
                TypeUtils.GetMethod(() => QueryableExtensions.Delete<T>(default(IQueryable<T>))),
                Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
            );

            return ((IQueryProvider)source.Provider).ExecuteEx<int>(expression);
        }

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

		//

		[RewriteAsync(true)]
		private static U Min<T, U>(this IQueryable<T> source, Expression<Func<T, U>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Min(default(IQueryable<T>), c => default(U))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<U>(expression);
		}

		[RewriteAsync(true)]
		private static U Max<T, U>(this IQueryable<T> source, Expression<Func<T, U>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Max(default(IQueryable<T>), c => default(U))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<U>(expression);
		}

		//

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
		private static int Sum<T>(this IQueryable<T> source, Expression<Func<T, int>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(int))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<int>(expression);
		}

		[RewriteAsync(true)]
		private static int? Sum<T>(this IQueryable<T> source, Expression<Func<T, int?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(int?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<int?>(expression);
		}

		[RewriteAsync(true)]
		private static long Sum<T>(this IQueryable<T> source, Expression<Func<T, long>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(long))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<long>(expression);
		}

		[RewriteAsync(true)]
		private static long? Sum<T>(this IQueryable<T> source, Expression<Func<T, long?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(long?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<long?>(expression);
		}

		[RewriteAsync(true)]
		private static float Sum<T>(this IQueryable<T> source, Expression<Func<T, float>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(float))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<float>(expression);
		}

		[RewriteAsync(true)]
		private static float? Sum<T>(this IQueryable<T> source, Expression<Func<T, float?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(float?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<float?>(expression);
		}

		[RewriteAsync(true)]
		private static double Sum<T>(this IQueryable<T> source, Expression<Func<T, double>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(double))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<double>(expression);
		}

		[RewriteAsync(true)]
		private static double? Sum<T>(this IQueryable<double?> source, Expression<Func<T, double?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(double?))), source.Expression, Expression.Quote(selector));

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

		//

		[RewriteAsync(true)]
		private static int Average<T>(this IQueryable<T> source, Expression<Func<T, int>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(int))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<int>(expression);
		}

		[RewriteAsync(true)]
		private static int? Average<T>(this IQueryable<T> source, Expression<Func<T, int?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(int?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<int?>(expression);
		}

		[RewriteAsync(true)]
		private static long Average<T>(this IQueryable<T> source, Expression<Func<T, long>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(long))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<long>(expression);
		}

		[RewriteAsync(true)]
		private static long? Average<T>(this IQueryable<T> source, Expression<Func<T, long?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(long?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<long?>(expression);
		}

		[RewriteAsync(true)]
		private static float Average<T>(this IQueryable<T> source, Expression<Func<T, float>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(float))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<float>(expression);
		}

		[RewriteAsync(true)]
		private static float? Average<T>(this IQueryable<T> source, Expression<Func<T, float?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(float?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<float?>(expression);
		}

		[RewriteAsync(true)]
		private static double Average<T>(this IQueryable<T> source, Expression<Func<T, double>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(double))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<double>(expression);
		}

		[RewriteAsync(true)]
		private static double? Average<T>(this IQueryable<double?> source, Expression<Func<T, double?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(double?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).ExecuteEx<double?>(expression);
		}
		
		public static IQueryable<T> WhereForUpdate<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => QueryableExtensions.WhereForUpdate(default(IQueryable<T>), c => true)), source.Expression, Expression.Quote(predicate));

			return ((IQueryProvider)source.Provider).ExecuteEx<IQueryable<T>>(expression);
		}
		
		public static IQueryable<U> SelectForUpdate<T, U>(this IQueryable<T> source, Expression<Func<T, U>> predicate)
			where T : DataAccessObject
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => QueryableExtensions.SelectForUpdate(default(IQueryable<T>), c => default(U))), source.Expression, Expression.Quote(predicate));

			return ((IQueryProvider)source.Provider).ExecuteEx<IQueryable<U>>(expression);
		}

		public static T IncludedItems<T>(this IQueryable<T> source)
			where T : DataAccessObject
        {
            Expression expression = Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), source.Expression);

            return source.Provider.ExecuteEx<T>(expression);
		}

		public static IQueryable<T> Include<T, U>(this IQueryable<T> source, Expression<Func<T, U>> include)
		{
		    Expression expression = Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T), typeof(U)), new[] { source.Expression, Expression.Quote(include) });

            return source.Provider.CreateQuery<T>(expression);
		}
	}
}
