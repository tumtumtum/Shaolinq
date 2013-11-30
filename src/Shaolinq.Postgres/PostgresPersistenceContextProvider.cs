// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Postgres
{
	public class PostgresPersistenceContextProvider
		: PersistenceContextProvider
	{
		public PostgresPersistenceContextProvider(string contextName, string databaseName, PostgresDatabaseConnectionInfo[] connectionInfos)
			: base(contextName)
		{
			foreach (var connectionInfo in connectionInfos)
			{
				this.AddPersistenceContext(connectionInfo.PersistenceMode, new PostgresPersistenceContext
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
					connectionInfo.NativeUuids,
					connectionInfo.CommandTimeout,
					connectionInfo.SchemaNamePrefix,
					connectionInfo.DateTimeKindIfUnspecified
				));
			}
		}
	}
}
