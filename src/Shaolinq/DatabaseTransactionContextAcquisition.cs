// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class DatabaseTransactionContextAcquisition
		: IDisposable
	{
		public TransactionContext TransactionContext { get; private set; }
		public SqlDatabaseContext SqlDatabaseContext { get; private set; }
		public SqlTransactionalCommandsContext SqlDatabaseCommandsContext { get; private set; }

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
