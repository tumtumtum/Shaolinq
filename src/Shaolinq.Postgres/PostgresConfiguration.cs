// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres
{
	public static class PostgresConfiguration
	{
		public static DataAccessModelConfiguration Create(
			string databaseName,
			string serverName,
			string userId,
			string password,
			bool poolConnections = PostgresSharedSqlDatabaseContextInfo.DefaultPooling,
			string categories = null,
			int port = PostgresSharedSqlDatabaseContextInfo.DefaultPostgresPort
			)
		{
			return Create(new PostgresSqlDatabaseContextInfo
			{
				DatabaseName = databaseName,
				Categories = categories,
				ServerName = serverName,
				Port = port,
				Pooling = poolConnections,
				UserId = userId,
				Password = password
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
