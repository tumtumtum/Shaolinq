// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

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
			return CreateConfiguration(contextName, databaseName, serverName, userName, password, poolConnections, PersistenceMode.ReadWrite);
		}

		public static DataAccessModelConfiguration CreateConfiguration(string contextName, string databaseName, string serverName, string userName, string password,  bool poolConnections, PersistenceMode persistenceMode)
		{
			return new DataAccessModelConfiguration()
			{
				PersistenceContexts = new PersistenceContextInfo[]
				{
					new MySqlPersistenceContextInfo()
					{
						ContextName = contextName,
						DatabaseName = databaseName,
						DatabaseConnectionInfos = new[]
						{
							new MySqlDatabaseConnectionInfo()
							{
								PersistenceMode = persistenceMode,
								ServerName = serverName,
								PoolConnections = true,
								UserName = userName,
								Password = password
							},
						}
					}
				}
			};
		}
	}
}
