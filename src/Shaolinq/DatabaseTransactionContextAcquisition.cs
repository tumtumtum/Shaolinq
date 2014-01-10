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
		public SqlDatabaseTransactionContext SqlDatabaseTransactionContext { get; private set; }

		public DatabaseTransactionContextAcquisition(TransactionContext transactionContext, SqlDatabaseContext sqlDatabaseContext, SqlDatabaseTransactionContext sqlDatabaseTransactionContext)
		{
			this.TransactionContext = transactionContext;
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.SqlDatabaseTransactionContext = sqlDatabaseTransactionContext;
		}

		public void SetWasError()
		{
		}

		public void Dispose()
		{
			if (this.TransactionContext.Transaction == null)
			{
				this.SqlDatabaseTransactionContext.Dispose();
			}
		}
	}
}
