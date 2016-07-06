// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
		private static Dictionary<RuntimeTypeHandle, Func<IDbTransaction, CancellationToken, Task>> commitAsyncFuncsByType = new Dictionary<RuntimeTypeHandle, Func<IDbTransaction, CancellationToken, Task>>();
		private static Dictionary<RuntimeTypeHandle, Func<IDbTransaction, CancellationToken, Task>> rollbackAsyncFuncsByType = new Dictionary<RuntimeTypeHandle, Func<IDbTransaction, CancellationToken, Task>>();

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
			Func<IDbTransaction, CancellationToken, Task> func;
			var typeHandle = Type.GetTypeHandle(transaction);

			if (!rollbackAsyncFuncsByType.TryGetValue(typeHandle, out func))
			{
				var type = Type.GetTypeFromHandle(typeHandle);
				var param1 = Expression.Parameter(typeof(IDbTransaction));
				var param2 = Expression.Parameter(typeof(CancellationToken));

				var method1 = type.GetMethod("RollbackAsync", new Type[0]);
				var method2 = type.GetMethod("RollbackAsync", new[] { typeof(CancellationToken) });

				if (method1 != null)
				{
					func = Expression.Lambda<Func<IDbTransaction, CancellationToken, Task>>(Expression.Call(Expression.Convert(param1, type), method1), param1, param2).Compile();
				}
				else if (method2 != null)
				{
					func = Expression.Lambda<Func<IDbTransaction, CancellationToken, Task>>(Expression.Call(Expression.Convert(param1, type), method2, param2), param1, param2).Compile();
				}
				else
				{
					func = Expression.Lambda<Func<IDbTransaction, CancellationToken, Task>>
					(
						Expression.Call
						(
							TypeUtils.GetMethod(() => Task.FromResult<object>(null)),
							Expression.Call(Expression.Convert(param1, type), "Rollback", null)
						), 
						param1, 
						param2
					).Compile();
				}

				rollbackAsyncFuncsByType = new Dictionary<RuntimeTypeHandle, Func<IDbTransaction,CancellationToken, Task>>(rollbackAsyncFuncsByType) { [typeHandle] = func };
			}

			return func(transaction, cancellationToken);
		}
		
		public static void CommitEx(this IDbTransaction transaction)
		{
			transaction.Commit();
		}

		public static Task CommitExAsync(this IDbTransaction transaction)
		{
			return transaction.CommitExAsync(CancellationToken.None);
		}

		public static Task CommitExAsync(this IDbTransaction transaction, CancellationToken cancellationToken)
		{
			Func<IDbTransaction, CancellationToken, Task> func;
			var typeHandle = Type.GetTypeHandle(transaction);

			if (!commitAsyncFuncsByType.TryGetValue(typeHandle, out func))
			{
				var type = transaction.GetType();
				var param1 = Expression.Parameter(typeof(IDbTransaction));
				var param2 = Expression.Parameter(typeof(CancellationToken));

				var method1 = type.GetMethod("CommitAsync", new Type[0]);
				var method2 = type.GetMethod("CommitAsync", new[] { typeof(CancellationToken) });

				if (method1 != null)
				{
					func = Expression.Lambda<Func<IDbTransaction, CancellationToken, Task>>(Expression.Call(Expression.Convert(param1, type), method1), param1, param2).Compile();
				}
				else if (method2 != null)
				{
					func = Expression.Lambda<Func<IDbTransaction, CancellationToken, Task>>(Expression.Call(Expression.Convert(param1, type), method2, param2), param1, param2).Compile();
				}
				else
				{
					func = Expression.Lambda<Func<IDbTransaction, CancellationToken, Task>>
					(
						Expression.Block
						(
							Expression.Call(Expression.Convert(param1, type), "Commit", null),
							Expression.Call(TypeUtils.GetMethod(() => Task.FromResult<object>(null)), Expression.Constant(null))
						),
						param1,
						param2
					).Compile();
				}

				commitAsyncFuncsByType = commitAsyncFuncsByType.Clone(typeHandle, func);
			}

			return func(transaction, cancellationToken);
		}
	}
}