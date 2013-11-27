// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Postgres.Devart
{
	public class DevartPostgresPersistenceContextProvider
		: PersistenceContextProvider
	{
		public DevartPostgresPersistenceContextProvider(string contextName, string databaseName, DevartPostgresDatabaseConnectionInfo[] connectionInfos)
			: base(contextName)
		{
			foreach (var connectionInfo in connectionInfos)
			{
				AddPersistenceContext(connectionInfo.PersistenceMode, new DevartPostgresPersistenceContext
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
