// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public static class MySqlConfiguration
	{
		public static DataAccessModelConfiguration Create(string connectionString)
		{
			return new DataAccessModelConfiguration
			{
				SqlDatabaseContextInfos = new List<SqlDatabaseContextInfo>
				{
					new MySqlSqlDatabaseContextInfo
					{
						ConnectionString = connectionString
					}
				},
				ConstraintDefaultsConfiguration = { IndexedStringMaximumLength = 255 }
			};
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password)
		{
			return Create(databaseName, serverName, userName, password, true);
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password,  bool poolConnections)
		{
			return Create(databaseName, serverName, userName, password, poolConnections, null);
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password,  bool poolConnections, string categories)
		{
			var retval = new DataAccessModelConfiguration
			{
				SqlDatabaseContextInfos = new List<SqlDatabaseContextInfo>
				{
					new MySqlSqlDatabaseContextInfo
					{
						DatabaseName = databaseName,
						Categories = categories,
						ServerName = serverName,
						PoolConnections = poolConnections,
						UserName = userName,
						Password = password
					}
				},
				ConstraintDefaultsConfiguration = { IndexedStringMaximumLength = 255 }
			};
			
			return retval;
		}
	}
}
