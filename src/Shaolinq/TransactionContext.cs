// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class TransactionContext
		: IEnlistmentNotification, IDisposable
	{
		public Transaction Transaction { get; set; }
		public BaseDataAccessModel DataAccessModel { get; private set; }

		public DataAccessObjectDataContext CurrentDataContext
		{
			get
			{
				if (this.dataAccessObjectDataContext == null)
				{
					this.dataAccessObjectDataContext = new DataAccessObjectDataContext(this.DataAccessModel, this.Transaction == null);
				}

				return this.dataAccessObjectDataContext;
			}
		}
		internal DataAccessObjectDataContext dataAccessObjectDataContext;

		public TransactionContext(BaseDataAccessModel dataAccessModel, Transaction transaction)
		{
			this.DataAccessModel = dataAccessModel;
			this.Transaction = transaction;

			persistenceTransactionContextsByStoreContexts = new Dictionary<PersistenceContext, TransactionEntry>(PrimeNumbers.Prime7);
		}

		internal struct TransactionEntry
		{
			public PersistenceTransactionContext persistenceTransactionContext;

			public TransactionEntry(PersistenceTransactionContext value)
			{
				this.persistenceTransactionContext = value;
			}
		}

		internal readonly IDictionary<PersistenceContext, TransactionEntry> persistenceTransactionContextsByStoreContexts;

		public virtual PersistenceTransactionContextAcquisition AcquirePersistenceTransactionContext(PersistenceContext persistenceContext)
		{
			PersistenceTransactionContext retval;

			if (this.Transaction == null)
			{
				retval = persistenceContext.NewDataTransactionContext(this.DataAccessModel, null);

				return new PersistenceTransactionContextAcquisition(this, persistenceContext, retval);
			}
			else
			{
				TransactionEntry outValue;
			
				if (persistenceTransactionContextsByStoreContexts.TryGetValue(persistenceContext, out outValue))
				{
					retval = outValue.persistenceTransactionContext;
				}
				else
				{
					retval = persistenceContext.NewDataTransactionContext(this.DataAccessModel, this.Transaction);

					persistenceTransactionContextsByStoreContexts[persistenceContext] = new TransactionEntry(retval);
				}

				return new PersistenceTransactionContextAcquisition(this, persistenceContext, retval);
			}
		}

		public void FlushConnections()
		{
			foreach (var persistenceTransactionContext in persistenceTransactionContextsByStoreContexts.Values)
			{
				try
				{
					persistenceTransactionContext.persistenceTransactionContext.Dispose();
				}
				catch
				{
				}
			}
		}

		private int disposed = 0;

		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
			{
				Exception rethrowException = null;

				foreach (var persistenceTransactionContext in persistenceTransactionContextsByStoreContexts.Values)
				{
					try
					{
						persistenceTransactionContext.persistenceTransactionContext.Dispose();
					}
					catch (Exception e)
					{
						rethrowException = e;
					}
				}

				if (rethrowException != null)
				{
					throw rethrowException;
				}
			}
		}

		public virtual void Commit(Enlistment enlistment)
		{
			enlistment.Done();
		}

		public virtual void InDoubt(Enlistment enlistment)
		{
			enlistment.Done();
		}

		public virtual void Prepare(PreparingEnlistment preparingEnlistment)
		{
			try
			{
				if (this.CurrentDataContext != null)
				{
					this.CurrentDataContext.Commit(this, false);
				}

				foreach (var persistenceTransactionContext in persistenceTransactionContextsByStoreContexts.Values)
				{
					persistenceTransactionContext.persistenceTransactionContext.Commit();
				}

				preparingEnlistment.Done();

				Dispose();
			}
			catch (Exception e)
			{
				foreach (var persistenceTransactionContext in persistenceTransactionContextsByStoreContexts.Values)
				{
					persistenceTransactionContext.persistenceTransactionContext.Rollback();
				}

				preparingEnlistment.ForceRollback(e);

				Dispose();
			}
		}

		public virtual void Rollback(Enlistment enlistment)
		{
			foreach (var persistenceTransactionContext in persistenceTransactionContextsByStoreContexts.Values)
			{
				persistenceTransactionContext.persistenceTransactionContext.Rollback();
			}

			Dispose();

			enlistment.Done();
		}
	}
}
