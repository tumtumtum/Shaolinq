// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	public static class PostgresConfiguration
	{
		public static DataAccessModelConfiguration Create(string connectionString)
		{
			return new DataAccessModelConfiguration
			{
				SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
       			{
       				new PostgresSqlDatabaseContextInfo
       				{
       					ConnectionString = connectionString
       				},
       			}
			};
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userId, string password, bool poolConnections = PostgresSqlDatabaseContextInfo.DefaultPooling, string categories = null, int port = PostgresSqlDatabaseContextInfo.DefaultPostgresPort, int commandTimeout = SqlDatabaseContextInfo.DefaultCommandTimeout, int connectionTimeout = SqlDatabaseContextInfo.DefaultConnectionTimeout, bool backendTimeouts = true)
		{
			return Create(new PostgresSqlDatabaseContextInfo
			{
				DatabaseName = databaseName,
				Categories = categories,
				ServerName = serverName,
				Port = port,
				Pooling = poolConnections,
				UserId = userId,
				Password = password,
				ConnectionCommandTimeout = commandTimeout,
				ConnectionTimeout = connectionTimeout,
				BackendTimeouts = backendTimeouts
			});
		}

		public static DataAccessModelConfiguration Create(PostgresSqlDatabaseContextInfo contextInfo)
		{
			return new DataAccessModelConfiguration
			{
				SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
				{
					contextInfo
				}
			};
		}
	}
}
