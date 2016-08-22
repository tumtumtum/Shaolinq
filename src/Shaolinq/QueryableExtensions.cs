// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.TypeBuilding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InvokeAsExtensionMethod

namespace Shaolinq
{
	public static partial class QueryableExtensions
	{
		internal static IQueryable<T> InsertHelper<T>(this IQueryable<T> source, Expression<Action<T>> updated, bool requiresIdentityInsert)
		{
			return source.Provider.CreateQuery<T>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), source.Expression, Expression.Constant(requiresIdentityInsert)));
		}

		internal static IQueryable<T> UpdateHelper<T>(this IQueryable<T> source, Expression<Action<T>> updated, bool requiresIdentityInsert)
		{
			return source.Provider.CreateQuery<T>(Expression.Call(null,((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), source.Expression, Expression.Constant(requiresIdentityInsert)));
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static bool Any<T>(this IQueryable<T> source)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.Any<T>(default(IQueryable<T>))),
				source.Expression
			);

			return ((IQueryProvider)source.Provider).Execute<bool>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static bool Any<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.Any<T>(default(IQueryable<T>))),
				Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
			);

			return ((IQueryProvider)source.Provider).Execute<bool>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static bool All<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.All<T>(default(IQueryable<T>), default(Expression<Func<T, bool>>))),
				source.Expression,
				Expression.Quote(predicate)
			);

			return ((IQueryProvider)source.Provider).Execute<bool>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T First<T>(this IQueryable<T> source)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.First<T>(default(IQueryable<T>))),
				source.Expression
			);

			return ((IQueryProvider)source.Provider).Execute<T>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T First<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.First<T>(default(IQueryable<T>))),
				Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
			);

			return ((IQueryProvider)source.Provider).Execute<T>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T FirstOrDefault<T>(this IQueryable<T> source)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.FirstOrDefault<T>(default(IQueryable<T>))),
				source.Expression
			);

			return ((IQueryProvider)source.Provider).Execute<T>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T FirstOrDefault<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.FirstOrDefault<T>(default(IQueryable<T>))),
				Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
			);

			return ((IQueryProvider)source.Provider).Execute<T>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T Single<T>(this IQueryable<T> source)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.Single<T>(default(IQueryable<T>))),
				source.Expression
			);

			return ((IQueryProvider)source.Provider).Execute<T>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T Single<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.Single<T>(default(IQueryable<T>))),
				Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
			);

			return ((IQueryProvider)source.Provider).Execute<T>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T SingleOrDefault<T>(this IQueryable<T> source)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.SingleOrDefault<T>(default(IQueryable<T>))),
				source.Expression
			);

			return ((IQueryProvider)source.Provider).Execute<T>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T SingleOrDefault<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.SingleOrDefault<T>(default(IQueryable<T>))),
				Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
			);

			return ((IQueryProvider)source.Provider).Execute<T>(expression);
		}

		[RewriteAsync]
		public static int Delete<T>(this IQueryable<T> source)
			where T : DataAccessObject
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => QueryableExtensions.Delete<T>(default(IQueryable<T>))), source.Expression);

			((SqlQueryProvider)source.Provider).DataAccessModel.Flush();

			return ((IQueryProvider)source.Provider).Execute<int>(expression);
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

			((SqlQueryProvider)source.Provider).DataAccessModel.Flush();

			return ((IQueryProvider)source.Provider).Execute<int>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static int Count<T>(this IQueryable<T> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Count(default(IQueryable<T>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<int>(expression);
		}
		
		[RewriteAsync(MethodAttributes.Public)]
		private static int Count<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.Count<T>(default(IQueryable<T>))),
				Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
			);

			return ((IQueryProvider)source.Provider).Execute<int>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static long LongCount<T>(this IQueryable<T> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.LongCount(default(IQueryable<T>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<long>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static long LongCount<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			Expression expression = Expression.Call
			(
				TypeUtils.GetMethod(() => Queryable.LongCount<T>(default(IQueryable<T>))),
				Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof(T)), source.Expression, Expression.Quote(predicate))
			);

			return ((IQueryProvider)source.Provider).Execute<long>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T Min<T>(this IQueryable<T> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Min(default(IQueryable<T>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<T>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T Max<T>(this IQueryable<T> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Max(default(IQueryable<T>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<T>(expression);
		}

		//

		[RewriteAsync(MethodAttributes.Public)]
		private static U Min<T, U>(this IQueryable<T> source, Expression<Func<T, U>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Min(default(IQueryable<T>), c => default(U))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<U>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static U Max<T, U>(this IQueryable<T> source, Expression<Func<T, U>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Max(default(IQueryable<T>), c => default(U))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<U>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static int Sum(this IQueryable<int> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<int>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<int>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static int? Sum(this IQueryable<int?> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<int?>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<int?>(expression);
		}
		
		[RewriteAsync(MethodAttributes.Public)]
		private static long Sum(this IQueryable<long> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<long>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<long>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static long? Sum(this IQueryable<long?> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<long?>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<long?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static float Sum(this IQueryable<float> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<float>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<float>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static float? Sum(this IQueryable<float?> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<float?>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<float?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static double Sum(this IQueryable<double> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<double>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<double>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static double? Sum(this IQueryable<double?> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<double?>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<double?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static decimal Sum(this IQueryable<decimal> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<decimal>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<decimal>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static decimal? Sum(this IQueryable<decimal?> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<decimal?>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<decimal?>(expression);
		}

		//

		[RewriteAsync(MethodAttributes.Public)]
		private static int Sum<T>(this IQueryable<T> source, Expression<Func<T, int>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(int))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<int>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static int? Sum<T>(this IQueryable<T> source, Expression<Func<T, int?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(int?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<int?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static long Sum<T>(this IQueryable<T> source, Expression<Func<T, long>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(long))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<long>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static long? Sum<T>(this IQueryable<T> source, Expression<Func<T, long?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(long?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<long?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static float Sum<T>(this IQueryable<T> source, Expression<Func<T, float>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(float))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<float>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static float? Sum<T>(this IQueryable<T> source, Expression<Func<T, float?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(float?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<float?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static double Sum<T>(this IQueryable<T> source, Expression<Func<T, double>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(double))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<double>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static double? Sum<T>(this IQueryable<double?> source, Expression<Func<T, double?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(double?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<double?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static decimal Sum<T>(this IQueryable<T> source, Expression<Func<T, decimal>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(decimal))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<decimal>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static decimal? Sum<T>(this IQueryable<decimal?> source, Expression<Func<T, decimal?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default(IQueryable<T>), c => default(decimal?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<decimal?>(expression);
		}

		//

		[RewriteAsync(MethodAttributes.Public)]
		private static int Average(this IQueryable<int> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<int>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<int>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static int? Average(this IQueryable<int?> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<int?>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<int?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static long Average(this IQueryable<long> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<long>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<long>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static long? Average(this IQueryable<long?> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<long?>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<long?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static float Average(this IQueryable<float> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<float>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<float>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static float? Average(this IQueryable<float?> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<float?>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<float?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static double Average(this IQueryable<double> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<double>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<double>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static double? Average(this IQueryable<double?> source)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<double?>))), source.Expression);

			return ((IQueryProvider)source.Provider).Execute<double?>(expression);
		}

		//

		[RewriteAsync(MethodAttributes.Public)]
		private static int Average<T>(this IQueryable<T> source, Expression<Func<T, int>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(int))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<int>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static int? Average<T>(this IQueryable<T> source, Expression<Func<T, int?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(int?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<int?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static long Average<T>(this IQueryable<T> source, Expression<Func<T, long>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(long))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<long>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static long? Average<T>(this IQueryable<T> source, Expression<Func<T, long?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(long?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<long?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static float Average<T>(this IQueryable<T> source, Expression<Func<T, float>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(float))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<float>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static float? Average<T>(this IQueryable<T> source, Expression<Func<T, float?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(float?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<float?>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static double Average<T>(this IQueryable<T> source, Expression<Func<T, double>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(double))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<double>(expression);
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static double? Average<T>(this IQueryable<double?> source, Expression<Func<T, double?>> selector)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default(IQueryable<T>), c => default(double?))), source.Expression, Expression.Quote(selector));

			return ((IQueryProvider)source.Provider).Execute<double?>(expression);
		}

		public static IQueryable<T> ForUpdate<T>(this IQueryable<T> source)
		{
			return source.Provider.CreateQuery<T>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), source.Expression));
		}
		
		public static T IncludedItems<T>(this IQueryable<T> source)
			where T : DataAccessObject
		{
			Expression expression = Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)), source.Expression);

			return source.Provider.Execute<T>(expression);
		}

		public static IQueryable<T> Include<T, U>(this IQueryable<T> source, Expression<Func<T, U>> include)
		{
			Expression expression = Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T), typeof(U)), new[] { source.Expression, Expression.Quote(include) });

			return source.Provider.CreateQuery<T>(expression);
		}
	}
}
