// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectPersistenceContextProvider
		: PersistenceContextProvider
	{
		public PostgresDotConnectPersistenceContextProvider(string contextName, string databaseName, PostgresDotConnectDatabaseConnectionInfo[] connectionInfos)
			: base(contextName)
		{
			foreach (var connectionInfo in connectionInfos)
			{
				AddPersistenceContext(connectionInfo.PersistenceMode, new PostgresDotConnectPersistenceContext
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
