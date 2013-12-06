// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.MySql
{
	public static class MySqlConfiguration
	{
		public static DataAccessModelConfiguration CreateConfiguration(string contextName, string databaseName, string serverName, string userName, string password)
		{
			return CreateConfiguration(contextName, databaseName, serverName, userName, password, true);
		}

		public static DataAccessModelConfiguration CreateConfiguration(string contextName, string databaseName, string serverName, string userName, string password,  bool poolConnections)
		{
			return CreateConfiguration(contextName, databaseName, serverName, userName, password, poolConnections, DatabaseReadMode.ReadWrite);
		}

		public static DataAccessModelConfiguration CreateConfiguration(string contextName, string databaseName, string serverName, string userName, string password,  bool poolConnections, DatabaseReadMode databaseReadMode)
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
