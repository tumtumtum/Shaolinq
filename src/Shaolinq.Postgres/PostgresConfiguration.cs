// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

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
			return new DataAccessModelConfiguration()
			{
				SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
				{
					new PostgresDatabaseContextInfo()
					{
						DatabaseName = databaseName,
						Categories = categories,
						ServerName = serverName,
						Pooling = true,
						UserId = userId,
						Password = password
					},
				}
			};
		}
	}
}
