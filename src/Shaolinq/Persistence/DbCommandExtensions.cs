// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using System.Data.Common;

namespace Shaolinq.Persistence
{
	public static partial class DbTransactionExtensions
	{
		[RewriteAsync]
		public static void RollbackEx(this IDbTransaction transaction)
		{
			var dbTransaction = transaction as DbTransaction;

			if (dbTransaction != null)
			{
				dbTransaction.Rollback();
			}

			transaction.Rollback();
		}

		[RewriteAsync]
		public static void CommitEx(this IDbTransaction transaction)
		{
			var dbTransaction = transaction as DbTransaction;

			if (dbTransaction != null)
			{
				dbTransaction.Commit();
			}

			transaction.Commit();
		}
	}

	public static partial class DbCommandExtensions
	{
		public static T Cast<T>(this IDbCommand command)
			where T : class, IDbCommand
		{
			return (command as T) ?? (T)((command as MarsDbCommand))?.Inner;
		}

		[RewriteAsync]
		public static IDataReader ExecuteReaderEx(this IDbCommand command)
		{
			var dbCommand = command.Cast<DbCommand>();

			if (dbCommand != null)
			{
				return dbCommand.ExecuteReader();
			}

			return command.ExecuteReader();
		}
		
		[RewriteAsync]
		public static int ExecuteNonQueryEx(this IDbCommand command)
		{
			var dbCommand = command.Cast<DbCommand>();

			if (dbCommand != null)
			{
				return dbCommand.ExecuteNonQuery();
			}

			return command.ExecuteNonQuery();
		}
	}
}
