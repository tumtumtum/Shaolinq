// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class PersistenceTransactionContextAcquisition
		: IDisposable
	{
		public TransactionContext TransactionContext { get; private set; }
		public PersistenceContext PersistenceContext { get; private set; }
		public PersistenceTransactionContext PersistenceTransactionContext { get; private set; }

		public PersistenceTransactionContextAcquisition(TransactionContext transactionContext, PersistenceContext persistenceContext, PersistenceTransactionContext persistenceTransactionContext)
		{
			this.TransactionContext = transactionContext;
			this.PersistenceContext = persistenceContext;
			this.PersistenceTransactionContext = persistenceTransactionContext;
		}

		public void SetWasError()
		{
		}

		public void Dispose()
		{
			if (this.TransactionContext.Transaction == null)
			{
				this.PersistenceTransactionContext.Dispose();
			}
		}
	}
}
