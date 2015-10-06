// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class DatabaseTransactionContextAcquisition
		: IDisposable
	{
		public TransactionContext TransactionContext { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; private set; }
		public SqlTransactionalCommandsContext SqlDatabaseCommandsContext { get; }

		public DatabaseTransactionContextAcquisition(TransactionContext transactionContext, SqlDatabaseContext sqlDatabaseContext, SqlTransactionalCommandsContext sqlDatabaseCommandsContext)
		{
			this.TransactionContext = transactionContext;
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.SqlDatabaseCommandsContext = sqlDatabaseCommandsContext;
		}

		public void SetWasError()
		{
		}

		public void Dispose()
		{
			if (this.TransactionContext.Transaction == null)
			{
				this.SqlDatabaseCommandsContext.Dispose();
			}
		}
	}
}
