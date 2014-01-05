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
		public SqlDatabaseContext SqlDatabaseContext { get; set; }
		public DataAccessModel DataAccessModel { get; private set; }
		internal readonly IDictionary<SqlDatabaseContext, TransactionEntry> persistenceTransactionContextsByStoreContexts;

		public DataAccessObjectDataContext CurrentDataContext
		{
			get
			{
				if (this.dataAccessObjectDataContext == null)
				{
					this.dataAccessObjectDataContext = new DataAccessObjectDataContext(this.DataAccessModel, this.DataAccessModel.GetCurrentSqlDatabaseContext(), this.Transaction == null);
				}

				return this.dataAccessObjectDataContext;
			}
		}
		internal DataAccessObjectDataContext dataAccessObjectDataContext;


		public string[] DatabaseContextCategories
		{
			get
			{
				return databaseContextCategories;
			}
			set
			{
				databaseContextCategories = value;

				if (value == null || value.Length == 0)
				{
					this.DatabaseContextCategoriesKey = ".";
				}
				else
				{
					this.DatabaseContextCategoriesKey = string.Join(",", value);
				}
			}
		}
		private string[] databaseContextCategories;

		public string DatabaseContextCategoriesKey { get; private set; }

		public TransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			this.DataAccessModel = dataAccessModel;
			this.Transaction = transaction;
			this.DatabaseContextCategoriesKey = ".";

			persistenceTransactionContextsByStoreContexts = new Dictionary<SqlDatabaseContext, TransactionEntry>(PrimeNumbers.Prime7);
		}

		internal struct TransactionEntry
		{
			public DatabaseTransactionContext databaseTransactionContext;

			public TransactionEntry(DatabaseTransactionContext value)
			{
				this.databaseTransactionContext = value;
			}
		}

		public virtual PersistenceTransactionContextAcquisition AcquirePersistenceTransactionContext(SqlDatabaseContext sqlDatabaseContext)
		{
			DatabaseTransactionContext retval;

			if (this.Transaction == null)
			{
				retval = sqlDatabaseContext.CreateDatabaseTransactionContext(this.DataAccessModel, null);

				return new PersistenceTransactionContextAcquisition(this, sqlDatabaseContext, retval);
			}
			else
			{
				TransactionEntry outValue;
			
				if (persistenceTransactionContextsByStoreContexts.TryGetValue(sqlDatabaseContext, out outValue))
				{
					retval = outValue.databaseTransactionContext;
				}
				else
				{
					retval = sqlDatabaseContext.CreateDatabaseTransactionContext(this.DataAccessModel, this.Transaction);

					persistenceTransactionContextsByStoreContexts[sqlDatabaseContext] = new TransactionEntry(retval);
				}

				return new PersistenceTransactionContextAcquisition(this, sqlDatabaseContext, retval);
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
				if (this.dataAccessObjectDataContext != null)
				{
					this.dataAccessObjectDataContext.Commit(this, false);
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
