// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;

namespace Shaolinq
{
	public static class DataAccessObjectsQueryableExtensions
	{
		internal static class MethodCache<T>
		{
			public static readonly MethodInfo DeleteMethod = typeof(DataAccessObjectsQueryableExtensions).GetMethod("DeleteWhere").MakeGenericMethod(typeof(T));
		}

		public static void DeleteWhere<T>(this DataAccessObjectsQueryable<T> queryable, Expression<Func<T, bool>> condition)
			where T : class, IDataAccessObject
		{
			queryable.DataAccessModel.Flush();

			var transactionContext = queryable.DataAccessModel.AmbientTransactionManager.GetCurrentContext(true);

			using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(queryable.DataAccessModel.GetCurrentSqlDatabaseContext()))
			{
				var expression = (Expression)Expression.Call(null, MethodCache<T>.DeleteMethod, Expression.Constant(queryable, typeof(DataAccessObjectsQueryable<T>)), condition);

				expression = Evaluator.PartialEval(queryable.DataAccessModel, expression);
				expression = QueryBinder.Bind(queryable.DataAccessModel, expression, queryable.ElementType, queryable.ExtraCondition);
				expression = ObjectOperandComparisonExpander.Expand(queryable.DataAccessModel, expression);
				expression = SqlQueryProvider.Optimize(queryable.DataAccessModel, expression);

				acquisition.SqlDatabaseCommandsContext.Delete((SqlDeleteExpression)expression);
			}
		}

		public static IEnumerable<T> DeleteObjectByObjectWhere<T>(this DataAccessObjectsQueryable<T> queryable, Expression<Func<T, bool>> condition)
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

				yield return value;
			}
		}
	}
}
