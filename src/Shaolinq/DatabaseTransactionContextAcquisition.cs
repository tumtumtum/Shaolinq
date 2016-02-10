// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class DatabaseTransactionContextAcquisition
		: IDisposable
	{
		public event EventHandler Disposed;

		public TransactionContext TransactionContext { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }
		public SqlTransactionalCommandsContext SqlDatabaseCommandsContext { get; }

		public DatabaseTransactionContextAcquisition(TransactionContext transactionContext, SqlDatabaseContext sqlDatabaseContext, SqlTransactionalCommandsContext sqlDatabaseCommandsContext)
		{
			this.TransactionContext = transactionContext;
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.SqlDatabaseCommandsContext = sqlDatabaseCommandsContext;
		}

		public void Dispose()
		{
			this.Disposed?.Invoke(this, EventArgs.Empty);
		}
	}
}
