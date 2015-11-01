// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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
			where T : DataAccessObject
		{
			queryable.DataAccessModel.Flush();

			var transactionContext = queryable.DataAccessModel.AmbientTransactionManager.GetCurrentContext(true);

			using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(queryable.DataAccessModel.GetCurrentSqlDatabaseContext()))
			{
				var expression = (Expression)Expression.Call(null, MethodCache<T>.DeleteMethod, Expression.Constant(queryable, typeof(DataAccessObjectsQueryable<T>)), condition);

				expression = Evaluator.PartialEval(expression);
				expression = QueryBinder.Bind(queryable.DataAccessModel, expression, queryable.ElementType, queryable.ExtraCondition);
				expression = SqlObjectOperandComparisonExpander.Expand(expression);
				expression = SqlQueryProvider.Optimize(expression, transactionContext.SqlDatabaseContext.SqlDataTypeProvider.GetTypeForEnums());

				acquisition.SqlDatabaseCommandsContext.Delete((SqlDeleteExpression)expression);
			}
		}

		public static IEnumerable<T> DeleteObjectByObjectWhere<T>(this DataAccessObjectsQueryable<T> queryable, Expression<Func<T, bool>> condition)
			where T : DataAccessObject
		{
			if (queryable.ExtraCondition != null)
			{
				var parameter = Expression.Parameter(typeof(T), "value");

				var body = Expression.AndAlso(condition.Body, queryable.ExtraCondition);

				condition = Expression.Lambda<Func<T, bool>>(body, parameter);
			}

			foreach (var value in queryable.Where(condition))
			{
				value.Delete();

				yield return value;
			}
		}
	}
}
