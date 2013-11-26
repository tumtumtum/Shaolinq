// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Persistence.Sql.MySql
{
	public static class MySqlConfiguration
	{
		public static DataAccessModelConfiguration CreateConfiguration(string contextName, string databaseName, string serverName, bool poolConnections, string userName, string password)
		{
			return CreateConfiguration(PersistenceMode.ReadWrite, contextName, databaseName, serverName, poolConnections, userName, password);
		}

		public static DataAccessModelConfiguration CreateConfiguration(PersistenceMode persistenceMode, string contextName, string databaseName, string serverName, bool poolConnections, string userName, string password)
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
