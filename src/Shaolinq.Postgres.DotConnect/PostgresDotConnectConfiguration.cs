// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 namespace Shaolinq.Postgres.DotConnect
{
    public static class PostgresDotConnectConfiguration
    {
		public static DataAccessModelConfiguration CreateConfiguration(string contextName, string databaseName, string serverName, string userId, string password)
		{
			return CreateConfiguration(contextName, databaseName, serverName, userId, password, true);
		}

        public static DataAccessModelConfiguration CreateConfiguration(string contextName, string databaseName, string serverName, string userId, string password,  bool poolConnections)
        {
			return CreateConfiguration(contextName, databaseName, serverName, userId, password, poolConnections, PersistenceMode.ReadWrite);
        }

        public static DataAccessModelConfiguration CreateConfiguration(string contextName, string databaseName, string serverName, string userId, string password,  bool poolConnections,  PersistenceMode persistenceMode)
        {
            return new DataAccessModelConfiguration()
            {
				PersistenceContexts = new PersistenceContextInfo[]
                {
                    new PostgresDotConnectPersistenceContextInfo()
                    {
                        ContextName = contextName,
                        DatabaseName = databaseName,
                        DatabaseConnectionInfos = new[]
						{
                            new PostgresDotConnectDatabaseConnectionInfo()
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
