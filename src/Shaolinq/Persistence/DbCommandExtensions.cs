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
				return await dbCommand.ExecuteReaderAsync();
			}

			return (DbDataReader)command.ExecuteReader();
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
				return await dbCommand.ExecuteNonQueryAsync();
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
