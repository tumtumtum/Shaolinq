using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerConfiguration
	{
		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password, bool deleteDatabaseDropsTablesOnly = false)
		{
			return Create(databaseName, serverName, userName, password, null);
		}

		public static DataAccessModelConfiguration Create(string connectionString, bool deleteDatabaseDropsTablesOnly = false)
		{
			return new DataAccessModelConfiguration
			{
				SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
				{
					new SqlServerSqlDatabaseContextInfo()
					{
						ConnectionString = connectionString,
						DeleteDatabaseDropsTablesOnly = deleteDatabaseDropsTablesOnly
					}
				}
			}; 
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password, string categories, bool deleteDatabaseDropsTablesOnly = false)
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
