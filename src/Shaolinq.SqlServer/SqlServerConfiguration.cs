using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerConfiguration
	{
		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password)
		{
			return Create(databaseName, serverName, userName, password, null);
		}

		public static DataAccessModelConfiguration Create(string databaseName, string serverName, string userName, string password, string categories)
		{
			return new DataAccessModelConfiguration()
			{
				SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
				{
					new SqlServerSqlDatabaseContextInfo()
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
