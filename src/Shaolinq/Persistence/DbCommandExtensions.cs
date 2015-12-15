// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.Persistence
{
	public static class DbCommandExtensions
	{
		public static async Task<IDataReader> ExecuteReaderAsync(this IDbCommand command)
		{
			var dbCommand = command as DbCommand;

			if (dbCommand != null)
			{
				return await dbCommand.ExecuteReaderAsync().ConfigureAwait(false);
			}

			return (DbDataReader)command.ExecuteReader();
		}

		public static async Task<IDataReader> ExecuteReaderAsync(this IDbCommand command, CancellationToken cancellationToken)
		{
			var dbCommand = command as DbCommand;

			if (dbCommand != null)
			{
				return await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
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
				return await dbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			}

			return command.ExecuteNonQuery();
		}
	}
}
