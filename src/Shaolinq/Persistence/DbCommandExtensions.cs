// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.Persistence
{
	public static class DbCommandExtensions
	{
		public static T Cast<T>(this IDbCommand command)
			where T : class, IDbCommand
		{
			return (command as T) ?? (T)((command as MarsDbCommand))?.Inner;
		}

		public static async Task<IDataReader> ExecuteReaderAsync(this IDbCommand command)
		{
			var dbCommand = command as DbCommand;

			if (dbCommand != null)
			{
				return await dbCommand.ExecuteReaderAsync();
			}

			return command.ExecuteReader();
		}

		public static async Task<IDataReader> ExecuteReaderAsync(this IDbCommand command, CancellationToken cancellationToken)
		{
			var dbCommand = command as DbCommand;

			if (dbCommand != null)
			{
				return await dbCommand.ExecuteReaderAsync(cancellationToken);
			}

			return command.ExecuteReader();
		}

		public static async Task<int> ExecuteNonQueryAsync(this IDbCommand command)
		{
			var dbCommand = command as DbCommand;

			if (dbCommand != null)
			{
				return await dbCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
			}

			return command.ExecuteNonQuery();
		}

		public static async Task<int> ExecuteNonQueryAsync(this IDbCommand command, CancellationToken cancellationToken)
		{
			var dbCommand = command as DbCommand;

			if (dbCommand != null)
			{
				return await dbCommand.ExecuteNonQueryAsync(cancellationToken);
			}

			return command.ExecuteNonQuery();
		}
	}
}
