namespace Shaolinq.Persistence.Sql.Sqlite
{
	public static class SqliteConfiguration
	{
		public static DataAccessModelConfiguration CreateConfiguration(string contextName, string fileName)
		{
			return CreateConfiguration(contextName, fileName, PersistenceMode.ReadWrite);
		}

		public static DataAccessModelConfiguration CreateConfiguration(string contextName, string fileName, PersistenceMode persistenceMode)
		{
			return new DataAccessModelConfiguration()
			{
		       	PersistenceContexts = new PersistenceContextInfo[]
	       		{
	       			new SqlitePersistenceContextInfo()
	       			{
	       				ContextName = contextName,
       					DatabaseConnectionInfos = new[]
       					{
       						new SqliteDatabaseConnectionInfo()
       						{
       							PersistenceMode = persistenceMode,
       							FileName = fileName
       						},
       					}
	       			}
	       		}
			};
		}
	}
}