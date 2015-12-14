using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.Persistence
{
	public static class DataReaderExtensions
	{
		public static async Task<bool> ReadAsync(this IDataReader reader)
		{
            var dbDataReader = reader as DbDataReader;

			if (dbDataReader != null)
			{
				return await dbDataReader.ReadAsync();
			}

			return reader.Read();
		}

		public static async Task<bool> ReadAsync(this IDataReader reader, CancellationToken cancellationToken)
		{
			var dbDataReader = reader as DbDataReader;

			if (dbDataReader != null)
			{
				return await dbDataReader.ReadAsync(cancellationToken);
			}

			return reader.Read();
		}
	}
}
