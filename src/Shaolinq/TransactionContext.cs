// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class TransactionContext
		: ISinglePhaseNotification, IDisposable
	{
		public Transaction Transaction { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; internal set; }
		public DataAccessModel DataAccessModel { get; }
		private readonly IDictionary<SqlDatabaseContext, TransactionEntry> persistenceTransactionContextsBySqlDatabaseContexts;

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
				return this.databaseContextCategories;
			}
			set
			{
				this.databaseContextCategories = value;

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
		public Guid ResourceManagerIdentifier { get; }

		public TransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			this.DataAccessModel = dataAccessModel;
			this.Transaction = transaction;
			this.DatabaseContextCategoriesKey = ".";
			this.ResourceManagerIdentifier = Guid.NewGuid();

			this.persistenceTransactionContextsBySqlDatabaseContexts = new Dictionary<SqlDatabaseContext, TransactionEntry>(PrimeNumbers.Prime7);
		}

		internal struct TransactionEntry
		{
			public SqlTransactionalCommandsContext sqlDatabaseCommandsContext;

			public TransactionEntry(SqlTransactionalCommandsContext value)
			{
				this.sqlDatabaseCommandsContext = value;
			}
		}

		public SqlTransactionalCommandsContext GetCurrentDatabaseTransactionContext(SqlDatabaseContext sqlDatabaseContext)
		{
			if (this.Transaction == null)
			{
				throw new InvalidOperationException("Transaction required");
			}

			return this.AcquirePersistenceTransactionContext(sqlDatabaseContext).SqlDatabaseCommandsContext;
		}

		public virtual DatabaseTransactionContextAcquisition AcquirePersistenceTransactionContext(SqlDatabaseContext sqlDatabaseContext)
		{
			SqlTransactionalCommandsContext retval;

			if (this.Transaction == null)
			{
				retval = sqlDatabaseContext.CreateSqlTransactionalCommandsContext(null);

				return new DatabaseTransactionContextAcquisition(this, sqlDatabaseContext, retval);
			}
			else
			{
				TransactionEntry outValue;
			
				if (this.persistenceTransactionContextsBySqlDatabaseContexts.TryGetValue(sqlDatabaseContext, out outValue))
				{
					retval = outValue.sqlDatabaseCommandsContext;
				}
				else
				{
					retval = sqlDatabaseContext.CreateSqlTransactionalCommandsContext(this.Transaction);

					this.persistenceTransactionContextsBySqlDatabaseContexts[sqlDatabaseContext] = new TransactionEntry(retval);
				}

				return new DatabaseTransactionContextAcquisition(this, sqlDatabaseContext, retval);
			}
		}

		public void FlushConnections()
		{
			foreach (var persistenceTransactionContext in this.persistenceTransactionContextsBySqlDatabaseContexts.Values)
			{
				try
				{
					persistenceTransactionContext.sqlDatabaseCommandsContext.Dispose();
				}
				catch
				{
				}
			}
		}

		private int disposed = 0;

		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref this.disposed, 1, 0) == 0)
			{
				Exception rethrowException = null;

				foreach (var persistenceTransactionContext in this.persistenceTransactionContextsBySqlDatabaseContexts.Values)
				{
					try
					{
						persistenceTransactionContext.sqlDatabaseCommandsContext.Dispose();
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
			try
			{
				// Don't properly support two-phase-commits yet
				// This could possibly still throw and fail

				foreach (var persistenceTransactionContext in this.persistenceTransactionContextsBySqlDatabaseContexts.Values)
				{
					persistenceTransactionContext.sqlDatabaseCommandsContext.Commit();
				}
			}
			catch (Exception e)
			{
				throw new TransactionAbortedException("The Shaolinq portion of the distributed transaction failed but other systems participating in the distributed transaction may have succeeded", e);
			}
			finally
			{
				enlistment.Done();

				this.Dispose();
			}
		}

		public virtual void InDoubt(Enlistment enlistment)
		{
			enlistment.Done();
		}

		public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
		{
			try
			{
				DataAccessModelTransactionManager.CurrentlyCommitingTransaction = this.Transaction;

				if (this.dataAccessObjectDataContext != null)
				{
					this.dataAccessObjectDataContext.Commit(this, false);
				}

				foreach (var persistenceTransactionContext in this.persistenceTransactionContextsBySqlDatabaseContexts.Values)
				{
					persistenceTransactionContext.sqlDatabaseCommandsContext.Commit();
				}

				singlePhaseEnlistment.Committed();
			}
			catch (Exception e)
			{
				foreach (var persistenceTransactionContext in this.persistenceTransactionContextsBySqlDatabaseContexts.Values)
				{
					try
					{
						persistenceTransactionContext.sqlDatabaseCommandsContext.Rollback();
					}
					catch
					{
					}
				}

				singlePhaseEnlistment.Aborted(e);
			}
			finally
			{
				DataAccessModelTransactionManager.CurrentlyCommitingTransaction = null;

				this.Dispose();
			}
		}

		public virtual void Prepare(PreparingEnlistment preparingEnlistment)
		{
			try
			{
				if (this.dataAccessObjectDataContext != null)
				{
					this.dataAccessObjectDataContext.Commit(this, false);
				}

				preparingEnlistment.Prepared();
			}
			catch (TransactionAbortedException)
			{
				throw;
			}
			catch (Exception e)
			{
				foreach (var persistenceTransactionContext in this.persistenceTransactionContextsBySqlDatabaseContexts.Values)
				{
					persistenceTransactionContext.sqlDatabaseCommandsContext.Rollback();
				}

				preparingEnlistment.ForceRollback(e);

				this.Dispose();
			}
		}

		public virtual void Rollback(Enlistment enlistment)
		{
			foreach (var persistenceTransactionContext in this.persistenceTransactionContextsBySqlDatabaseContexts.Values)
			{
				persistenceTransactionContext.sqlDatabaseCommandsContext.Rollback();
			}

			this.Dispose();

			enlistment.Done();
		}
	}
}
