// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerConfiguration
	{
		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password, bool deleteDatabaseDropsTablesOnly = false)
		{
			return Create(databaseName, serverName, userName, password, null);
		}

		public static DataAccessModelConfiguration Create(string connectionString, bool deleteDatabaseDropsTablesOnly = false, bool multipleActiveResultsets = false)
		{
			return new DataAccessModelConfiguration
			{
				SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
				{
					new SqlServerSqlDatabaseContextInfo()
					{
						ConnectionString = connectionString,
						DeleteDatabaseDropsTablesOnly = deleteDatabaseDropsTablesOnly,
						MultipleActiveResultSets = multipleActiveResultsets
					}
				}
			}; 
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName = null, string password = null, string categories = null, bool deleteDatabaseDropsTablesOnly = false, bool multipleActiveResultsets = false)
		{
			return new DataAccessModelConfiguration()
			{
				SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
				{
					new SqlServerSqlDatabaseContextInfo
					{
						DatabaseName = databaseName,
						Categories = categories,
						ServerName = serverName,
						UserName = userName,
						Password = password
					},
				}
			};
		}
	}
}
