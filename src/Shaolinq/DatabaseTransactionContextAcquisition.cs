// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class DatabaseTransactionContextAcquisition
		: IDisposable
	{
		public DataAccessModelTransactionContext TransactionContext { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }
		public SqlTransactionalCommandsContext SqlDatabaseCommandsContext { get; }

		public DatabaseTransactionContextAcquisition(DataAccessModelTransactionContext transactionContext, SqlDatabaseContext sqlDatabaseContext, SqlTransactionalCommandsContext sqlDatabaseCommandsContext)
		{
			this.TransactionContext = transactionContext;
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.SqlDatabaseCommandsContext = sqlDatabaseCommandsContext;
		}

		public void Dispose()
		{
			if (this.TransactionContext.DataAccessTransaction == null)
			{
				this.SqlDatabaseCommandsContext.Dispose();
			}
		}
	}
}
