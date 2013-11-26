// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence.Sql.Linq;
using Shaolinq.Persistence.Sql.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Optimizer;

namespace Shaolinq
{
	public static class DataAccessObjectsQueryableExtensions
	{
		internal static class MethodCache<T>
		{
			public static readonly MethodInfo DeleteMethod = typeof(DataAccessObjectsQueryableExtensions).GetMethod("DeleteImmediately").MakeGenericMethod(typeof(T));
		}

		public static MethodInfo GetDeleteMethod<T>()
		{
			return MethodCache<T>.DeleteMethod;
		}

		public static void DeleteImmediately<T>(this DataAccessObjectsQueryable<T> queryable, Expression<Func<T, bool>> condition)
			where T : class, IDataAccessObject
		{
			queryable.DataAccessModel.FlushCurrentTransaction();

			var transactionContext = queryable.DataAccessModel.AmbientTransactionManager.GetCurrentContext(false);

			using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(queryable.PersistenceContext))
			{
				var expression = (Expression)Expression.Call(null, MethodCache<T>.DeleteMethod, Expression.Constant(queryable, typeof(DataAccessObjectsQueryable<T>)), condition);

				expression = Evaluator.PartialEval(queryable.DataAccessModel, expression);
				expression = QueryBinder.Bind(queryable.DataAccessModel, expression, queryable.ElementType, queryable.ExtraCondition);
				expression = ObjectOperandComparisonExpander.Expand(expression);
				expression = SqlQueryProvider.Optimize(queryable.DataAccessModel, expression);

				acquisition.PersistenceTransactionContext.Delete((SqlDeleteExpression)expression);
			}
		}

		public static void DeleteDelayed<T>(this DataAccessObjectsQueryable<T> queryable, Expression<Func<T, bool>> condition)
			where T : class, IDataAccessObject
		{
			if (queryable.ExtraCondition != null)
			{
				var parameter = Expression.Parameter(typeof(T), "value");

				var body = Expression.AndAlso(condition.Body, queryable.ExtraCondition);

				condition = Expression.Lambda<Func<T, bool>>(body, parameter);
			}

			foreach (T value in queryable.Where(condition))
			{
				value.Delete();
			}
		}
	}
}
