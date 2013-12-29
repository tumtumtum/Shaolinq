// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public static class MySqlConfiguration
	{
		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password)
		{
			return Create(databaseName, serverName, userName, password, true);
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password,  bool poolConnections)
		{
			return Create(databaseName, serverName, userName, password, poolConnections, DatabaseReadMode.ReadWrite);
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password,  bool poolConnections, DatabaseReadMode databaseReadMode)
		{
			return new DataAccessModelConfiguration()
			{
				DatabaseConnectionInfos = new DatabaseConnectionInfo[]
				{
					new MySqlDatabaseConnectionInfo()
					{
						DatabaseName = databaseName,
						DatabaseReadMode = databaseReadMode,
						ServerName = serverName,
						PoolConnections = true,
						UserName = userName,
						Password = password
					},
				}
			};
		}
	}
}
