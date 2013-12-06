// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class PersistenceTransactionContextAcquisition
		: IDisposable
	{
		public TransactionContext TransactionContext { get; private set; }
		public DatabaseConnection DatabaseConnection { get; private set; }
		public DatabaseTransactionContext DatabaseTransactionContext { get; private set; }

		public PersistenceTransactionContextAcquisition(TransactionContext transactionContext, DatabaseConnection databaseConnection, DatabaseTransactionContext databaseTransactionContext)
		{
			this.TransactionContext = transactionContext;
			this.DatabaseConnection = databaseConnection;
			this.DatabaseTransactionContext = databaseTransactionContext;
		}

		public void SetWasError()
		{
		}

		public void Dispose()
		{
			if (this.TransactionContext.Transaction == null)
			{
				this.DatabaseTransactionContext.Dispose();
			}
		}
	}
}
