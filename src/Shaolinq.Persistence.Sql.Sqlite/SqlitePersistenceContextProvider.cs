using System.Collections.Generic;

namespace Shaolinq.Persistence.Sql.Sqlite
{
	public class SqlitePersistenceContextProvider
		: PersistenceContextProvider
	{
		public SqlitePersistenceContextProvider(string contextName, string databaseName, IEnumerable<SqliteDatabaseConnectionInfo> connectionInfos)
			: base(contextName)
		{
			foreach (var connectionInfo in connectionInfos)
			{
				AddPersistenceContext(connectionInfo.PersistenceMode, new SqlitePersistenceContext(connectionInfo.FileName, connectionInfo.SchemaNamePrefix));
			}
		}
	}
}