using System.Collections.Generic;

namespace Shaolinq.Persistence.Sql.MySql
{
	public class MySqlPersistenceContextProvider
		: PersistenceContextProvider
	{
		public MySqlPersistenceContextProvider(string contextName, string databaseName, IEnumerable<MySqlDatabaseConnectionInfo> connectionInfos)
			: base(contextName)
		{
			foreach (var connectionInfo in connectionInfos)
			{
				AddPersistenceContext(connectionInfo.PersistenceMode, new MySqlPersistenceContext(connectionInfo.ServerName, databaseName, connectionInfo.UserName, connectionInfo.Password, connectionInfo.PoolConnections, connectionInfo.SchemaNamePrefix));
			}
		}
	}
}
