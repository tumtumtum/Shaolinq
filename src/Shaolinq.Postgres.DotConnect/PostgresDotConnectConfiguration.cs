// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.Postgres.DotConnect
{
    public static class PostgresDotConnectConfiguration
    {
		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userId, string password)
		{
			return Create(databaseName, serverName, userId, password, true);
		}

        public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userId, string password,  bool poolConnections)
        {
			return Create(databaseName, serverName, userId, password, poolConnections, DatabaseReadMode.ReadWrite);
        }

        public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userId, string password,  bool poolConnections,  DatabaseReadMode databaseReadMode)
        {
            return new DataAccessModelConfiguration()
            {
				DatabaseConnectionInfos = new DatabaseConnectionInfo[]
				{
                    new PostgresDotConnectDatabaseConnectionInfo()
                    {
                        DatabaseReadMode = databaseReadMode,
						DatabaseName = databaseName,
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
