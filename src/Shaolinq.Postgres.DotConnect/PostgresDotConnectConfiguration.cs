// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Postgres.DotConnect
{
	public static class PostgresDotConnectConfiguration
	{
		public static DataAccessModelConfiguration Create(
			string databaseName,
			string serverName,
			string userId,
			string password,
			bool poolConnections = PostgresDotConnectSqlDatabaseContextInfo.DefaultPooling,
			string categories = null,
			int port = PostgresDotConnectSqlDatabaseContextInfo.DefaultPostgresPort,
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
				ConnectionCommandTimeout = commandTimeout,
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
