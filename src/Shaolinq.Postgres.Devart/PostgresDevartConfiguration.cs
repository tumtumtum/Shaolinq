// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 namespace Shaolinq.Postgres.Devart
{
    public static class PostgresDevartConfiguration
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
                    new PostgresDevartPersistenceContextInfo()
                    {
                        ContextName = contextName,
                        DatabaseName = databaseName,
                        DatabaseConnectionInfos = new[]
						{
                            new PostgresDevartDatabaseConnectionInfo()
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
