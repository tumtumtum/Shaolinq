// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Platform;

namespace Shaolinq.Persistence
{
	public static partial class DbTransactionExtensions
	{
		private static Dictionary<RuntimeTypeHandle, Func<IDbTransaction, Task>> commitAsyncFuncsByType = new Dictionary<RuntimeTypeHandle, Func<IDbTransaction, Task>>();
		private static Dictionary<RuntimeTypeHandle, Func<IDbTransaction, Task>> rollbackAsyncFuncsByType = new Dictionary<RuntimeTypeHandle, Func<IDbTransaction, Task>>();

		public static void RollbackEx(this IDbTransaction transaction)
		{
			transaction.Rollback();
		}

		public static Task RollbackExAsync(this IDbTransaction transaction)
		{
			return transaction.RollbackExAsync(CancellationToken.None);
		}

		public static Task RollbackExAsync(this IDbTransaction transaction, CancellationToken cancellationToken)
		{
			Func<IDbTransaction, Task> func;
			var typeHandle = Type.GetTypeHandle(transaction);

			if (!rollbackAsyncFuncsByType.TryGetValue(typeHandle, out func))
			{
				var type = Type.GetTypeFromHandle(typeHandle);
				var param1 = Expression.Parameter(typeof(IDbTransaction));
				var method = type.GetMethod("RollbackAsync");

				if (method != null)
				{
					func = Expression.Lambda<Func<IDbTransaction, Task>>(Expression.Call(Expression.Convert(param1, type), method), param1).Compile();
				}
				else
				{
					func = Expression.Lambda<Func<IDbTransaction, Task>>(Expression.Call(TypeUtils.GetMethod(() => Task.FromResult<object>(null)), Expression.Call(Expression.Convert(param1, type), "Rollback", null)), param1).Compile();
				}

				rollbackAsyncFuncsByType = new Dictionary<RuntimeTypeHandle, Func<IDbTransaction, Task>>(commitAsyncFuncsByType) { [typeHandle] = func };
			}

			return func(transaction);
		}
		
		public static void CommitEx(this IDbTransaction transaction)
		{
			transaction.Commit();
		}

		public static async Task CommitExAsync(this IDbTransaction transaction)
		{
			await transaction.CommitExAsync(CancellationToken.None);
		}

		public static async Task CommitExAsync(this IDbTransaction transaction, CancellationToken cancellationToken)
		{
			Func<IDbTransaction, Task> func;
			var typeHandle = Type.GetTypeHandle(transaction);

			if (!commitAsyncFuncsByType.TryGetValue(typeHandle, out func))
			{
				var type = transaction.GetType();
				var param1 = Expression.Parameter(typeof(IDbTransaction));

				var method = type.GetMethod("CommitAsync");

				if (method != null)
				{
					func = Expression.Lambda<Func<IDbTransaction, Task>>(Expression.Call(Expression.Convert(param1, type), method), param1).Compile();
				}
				else
				{
					func = Expression.Lambda<Func<IDbTransaction, Task>>(Expression.Call(TypeUtils.GetMethod(() => Task.FromResult<object>(null)), Expression.Call(Expression.Convert(param1, type), "Commit", null)), param1).Compile();
				}

				commitAsyncFuncsByType = new Dictionary<RuntimeTypeHandle, Func<IDbTransaction, Task>>(commitAsyncFuncsByType) { [typeHandle] = func };
			}

			await func(transaction);
		}
	}
}