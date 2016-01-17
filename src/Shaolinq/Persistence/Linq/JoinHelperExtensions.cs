// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Persistence.Linq
{
	internal static class JoinHelperExtensions
	{
		private static Expression GetSourceExpression<TSource>(IEnumerable<TSource> source)
		{
			var queryable = source as IQueryable<TSource>;

			return queryable?.Expression ?? Expression.Constant(source, typeof(IEnumerable<TSource>));
		}

		public static IQueryable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return outer.Provider.CreateQuery<TResult>(Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(TOuter), typeof(TInner), typeof(TKey), typeof(TResult)), outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector)));
		}
	}
}