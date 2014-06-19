// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	public static class PostgresConfiguration
	{
		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userId, string password)
		{
			return Create(databaseName, serverName, userId, password, true);
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userId, string password, bool poolConnections)
		{
			return Create(databaseName, serverName, userId, password, poolConnections, null);
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userId, string password, bool poolConnections, string categories)
		{
			return Create(new PostgresSqlDatabaseContextInfo
			{
				DatabaseName = databaseName,
				Categories = categories,
				ServerName = serverName,
				Pooling = poolConnections,
				UserId = userId,
				Password = password
			});
		}

		public static DataAccessModelConfiguration Create(PostgresSqlDatabaseContextInfo contextInfo)
		{
			return new DataAccessModelConfiguration
			{
				SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
				{
					contextInfo
				}
			};
		}
	}
}
