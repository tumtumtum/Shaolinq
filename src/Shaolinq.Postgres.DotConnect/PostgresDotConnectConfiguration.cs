// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres.DotConnect
{
	public static class PostgresDotConnectConfiguration
	{
		public static DataAccessModelConfiguration Create(
			string databaseName,
			string serverName,
			string userId,
			string password,
			bool poolConnections = PostgresSharedSqlDatabaseContextInfo.DefaultPooling,
			string categories = null,
			int port = PostgresSharedSqlDatabaseContextInfo.DefaultPostgresPort,
			bool unpreparedExecute = PostgresDotConnectSqlDatabaseContextInfo.DefaultUnpreparedExecute,
			int commandTimeout = SqlDatabaseContextInfo.DefaultCommandTimeout,
			int connectionTimeout = SqlDatabaseContextInfo.DefaultConnectionTimeout
			)
		{
			return Create(new PostgresDotConnectSqlDatabaseContextInfo()
			{
				Categories = categories,
				DatabaseName = databaseName,
				ServerName = serverName,
				Port = port,
				UserId = userId,
				Password = password,
				Pooling = poolConnections,
				UnpreparedExecute = unpreparedExecute,
				CommandTimeout = commandTimeout,
				ConnectionTimeout = connectionTimeout
			});
		}

		public static DataAccessModelConfiguration Create(PostgresDotConnectSqlDatabaseContextInfo contextInfo)
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
