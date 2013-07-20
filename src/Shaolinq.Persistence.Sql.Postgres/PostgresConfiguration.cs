namespace Shaolinq.Persistence.Sql.Postgres
{
	public static class PostgresConfiguration
	{
		public static DataAccessModelConfiguration CreateConfiguration(string contextName, string databaseName, string serverName, bool poolConnections, string userId, string password)
		{
			return CreateConfiguration(PersistenceMode.ReadWrite, contextName, databaseName, serverName, poolConnections, userId, password);
		}

		public static DataAccessModelConfiguration CreateConfiguration(PersistenceMode persistenceMode, string contextName, string databaseName, string serverName, bool poolConnections, string userId, string password)
		{
			return new DataAccessModelConfiguration()
			{
				PersistenceContexts = new PersistenceContextInfo[]
				{
					new PostgresPersistenceContextInfo()
					{
						ContextName = contextName,
						DatabaseName = databaseName,
						DatabaseConnectionInfos =
							new[]
							{
								new PostgresDatabaseConnectionInfo()
								{
									PersistenceMode = persistenceMode,
									ServerName = serverName,
									Pooling = true,
									UserId = userId,
									Password = password
								},
							}
					}
				}
			};
		}
	}
}
