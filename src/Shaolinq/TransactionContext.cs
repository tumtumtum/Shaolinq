// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
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
		public DataAccessModel DataAccessModel { get; private set; }

		public DatabaseConnection DatabaseConnection { get; set; }

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

		public TransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			this.DataAccessModel = dataAccessModel;
			this.Transaction = transaction;

			persistenceTransactionContextsByStoreContexts = new Dictionary<DatabaseConnection, TransactionEntry>(PrimeNumbers.Prime7);
		}

		internal struct TransactionEntry
		{
			public DatabaseTransactionContext databaseTransactionContext;

			public TransactionEntry(DatabaseTransactionContext value)
			{
				this.databaseTransactionContext = value;
			}
		}

		internal readonly IDictionary<DatabaseConnection, TransactionEntry> persistenceTransactionContextsByStoreContexts;

		public virtual PersistenceTransactionContextAcquisition AcquirePersistenceTransactionContext(DatabaseConnection databaseConnection)
		{
			DatabaseTransactionContext retval;

			if (this.Transaction == null)
			{
				retval = databaseConnection.NewDataTransactionContext(this.DataAccessModel, null);

				return new PersistenceTransactionContextAcquisition(this, databaseConnection, retval);
			}
			else
			{
				TransactionEntry outValue;
			
				if (persistenceTransactionContextsByStoreContexts.TryGetValue(databaseConnection, out outValue))
				{
					retval = outValue.databaseTransactionContext;
				}
				else
				{
					retval = databaseConnection.NewDataTransactionContext(this.DataAccessModel, this.Transaction);

					persistenceTransactionContextsByStoreContexts[databaseConnection] = new TransactionEntry(retval);
				}

				return new PersistenceTransactionContextAcquisition(this, databaseConnection, retval);
			}
		}

		public void FlushConnections()
		{
			foreach (var persistenceTransactionContext in persistenceTransactionContextsByStoreContexts.Values)
			{
				try
				{
					persistenceTransactionContext.databaseTransactionContext.Dispose();
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
						persistenceTransactionContext.databaseTransactionContext.Dispose();
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
					persistenceTransactionContext.databaseTransactionContext.Commit();
				}

				preparingEnlistment.Done();

				Dispose();
			}
			catch (Exception e)
			{
				foreach (var persistenceTransactionContext in persistenceTransactionContextsByStoreContexts.Values)
				{
					persistenceTransactionContext.databaseTransactionContext.Rollback();
				}

				preparingEnlistment.ForceRollback(e);

				Dispose();
			}
		}

		public virtual void Rollback(Enlistment enlistment)
		{
			foreach (var persistenceTransactionContext in persistenceTransactionContextsByStoreContexts.Values)
			{
				persistenceTransactionContext.databaseTransactionContext.Rollback();
			}

			Dispose();

			enlistment.Done();
		}
	}
}
