// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.Generic;

namespace Shaolinq.Sqlite
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
