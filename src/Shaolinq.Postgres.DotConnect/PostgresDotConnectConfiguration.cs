// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

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
			return Create(databaseName, serverName, userId, password, poolConnections, null);
        }

        public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userId, string password,  bool poolConnections,  string categories)
        {
            return new DataAccessModelConfiguration()
            {
				SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
				{
                    new PostgresDotConnectSqlDatabaseContextInfo()
                    {
                        Categories = categories,
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
