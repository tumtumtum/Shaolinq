// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 namespace Shaolinq.Postgres.Devart
{
	public class PostgresDevartPersistenceContextProvider
		: PersistenceContextProvider
	{
		public PostgresDevartPersistenceContextProvider(string contextName, string databaseName, PostgresDevartDatabaseConnectionInfo[] connectionInfos)
			: base(contextName)
		{
			foreach (var connectionInfo in connectionInfos)
			{
				AddPersistenceContext(connectionInfo.PersistenceMode, new PostgresDevartPersistenceContext
				(
					connectionInfo.ServerName,
					connectionInfo.UserId,
					connectionInfo.Password,
					databaseName,
					connectionInfo.Port,
					connectionInfo.Pooling,
					connectionInfo.MinPoolSize,
					connectionInfo.MaxPoolSize,
					connectionInfo.ConnectionTimeout,
					connectionInfo.CommandTimeout,
					connectionInfo.NativeUuids,
					connectionInfo.SchemaNamePrefix,
					connectionInfo.DateTimeKindIfUnspecified
				));
			}
		}
	}
}
