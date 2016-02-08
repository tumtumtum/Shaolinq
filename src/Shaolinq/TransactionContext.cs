// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Transactions;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class TransactionContext
		: ISinglePhaseNotification, IDisposable
	{
		internal static int GetCurrentContextVersion()
		{
			throw new NotImplementedException(MethodBase.GetCurrentMethod().Name);
		}

		private int version;
		private int versionNesting;
		private static readonly ConcurrentDictionary<Transaction, HashSet<DataAccessModel>> dataAccessModelsByTransaction = new ConcurrentDictionary<Transaction, HashSet<DataAccessModel>>(ObjectReferenceIdentityEqualityComparer<Transaction>.Default);

		internal static AsyncLocal<TransactionContext> Current { get; set; }

		internal int GetCurrentVersion()
		{
			return this.version;
		}

		internal static IEnumerable<DataAccessModel> GetCurrentlyEnlistedDataAccessModels()
		{
			var transaction = Transaction.Current;

			return transaction == null ? Enumerable.Empty<DataAccessModel>() : dataAccessModelsByTransaction[transaction];
		}

		internal class TransactionContextVersionContext
			: IDisposable
		{
			private readonly TransactionContext context;

			public int Version { get; }

			public TransactionContextVersionContext(TransactionContext context)
			{
				this.context = context;

				if (context.versionNesting == 0)
				{
					context.version++;
				}

				this.Version = context.version;

				context.versionNesting++;
			}

			public void Dispose() => this.context.versionNesting--;
		}

		private volatile bool disposed;
		public Transaction Transaction { get; }
		private readonly DataAccessModel dataAccessModel;
		public SqlDatabaseContext SqlDatabaseContext { get; internal set; }

		private DataAccessObjectDataContext dataAccessObjectDataContext;
		private readonly Dictionary<SqlDatabaseContext, SqlTransactionalCommandsContext> commandsContextsBySqlDatabaseContexts;

		internal TransactionContextVersionContext AcquireVersionContext()
		{
			return new TransactionContextVersionContext(this);
		}

		public static TransactionContext GetCurrentContext(DataAccessModel dataAccessModel, bool forWrite)
		{
			TransactionContext context;
			var transaction = Transaction.Current;

			if (transaction == null)
			{
				if (forWrite)
				{
					throw new InvalidOperationException("Write operation must be performed inside a transaction scope");
				}

				context = dataAccessModel.asyncLocalTransactionContext.Value;

				if (context == null)
				{
					context = new TransactionContext(null, dataAccessModel);

					dataAccessModel.asyncLocalTransactionContext.Value = context;
				}

				return context;
			}

			if (transaction.TransactionInformation.Status == TransactionStatus.Aborted)
			{
				throw new TransactionAbortedException();
			}

			var contexts = dataAccessModel.transactionContextsByTransaction;

			var skipTest = false;
			
			if (contexts == null)
			{
				skipTest = true;
				contexts = dataAccessModel.transactionContextsByTransaction = new ConcurrentDictionary<Transaction, TransactionContext>();
            }

			if (skipTest || !contexts.TryGetValue(transaction, out context))
			{
				context = new TransactionContext(transaction, dataAccessModel);
				contexts[transaction] = context;

				HashSet<DataAccessModel> dataAccessModels;

				if (!dataAccessModelsByTransaction.TryGetValue(transaction, out dataAccessModels))
				{
					dataAccessModels = dataAccessModelsByTransaction.GetOrAdd(transaction, new HashSet<DataAccessModel>());
				}

				dataAccessModels.Add(dataAccessModel);

				transaction.EnlistVolatile(context, EnlistmentOptions.None);
			}

			return context;
		}

		public DataAccessObjectDataContext GetCurrentDataContext()
		{
			if (dataAccessObjectDataContext == null)
			{
				dataAccessObjectDataContext = new DataAccessObjectDataContext(this.dataAccessModel, this.dataAccessModel.GetCurrentSqlDatabaseContext(), this.Transaction == null);
			}
			
			return this.dataAccessObjectDataContext;
		}
		
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
		
		public TransactionContext(Transaction transaction, DataAccessModel dataAccessModel)
		{
			this.dataAccessModel = dataAccessModel;
			this.Transaction = transaction;
			this.DatabaseContextCategoriesKey = ".";

			this.commandsContextsBySqlDatabaseContexts = new Dictionary<SqlDatabaseContext, SqlTransactionalCommandsContext>();
		}

		public SqlTransactionalCommandsContext GetCurrentTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext)
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

			if (!this.commandsContextsBySqlDatabaseContexts.TryGetValue(sqlDatabaseContext, out retval))
			{
				retval = sqlDatabaseContext.CreateSqlTransactionalCommandsContext(this.Transaction);

				this.commandsContextsBySqlDatabaseContexts[sqlDatabaseContext] = retval;
			}

			return new DatabaseTransactionContextAcquisition(this, sqlDatabaseContext, retval);
		}

		~TransactionContext()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (disposed)
			{
				return;
			}

			this.dataAccessModel.asyncLocalTransactionContext.Dispose();

			if (this.Transaction != null)
			{
				((IDictionary<Transaction, HashSet<DataAccessModel>>)dataAccessModelsByTransaction).Remove(this.Transaction);
				((IDictionary<Transaction, TransactionContext>)dataAccessModel.transactionContextsByTransaction).Remove(this.Transaction);
			}

			Exception rethrowException = null;

			foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
			{
				try
				{
					commandsContext.Dispose();
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

			disposed = true;
        }

		public virtual void Commit(Enlistment enlistment)
		{
			try
			{
				foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					commandsContext.Commit();
				}
			}
			catch (Exception e)
			{
				throw new TransactionAbortedException("The Shaolinq portion of the distributed transaction failed but other systems participating in the distributed transaction may have succeeded", e);
			}
			finally
			{
				try
				{
					enlistment.Done();
				}
				finally
				{
					this.Dispose();
				}
			}
		}

		public virtual void InDoubt(Enlistment enlistment)
		{
			try
			{
				enlistment.Done();
			}
			finally
			{
				this.Dispose();
			}
		}

		public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
		{
			var dispose = true;

			try
			{
				this.dataAccessObjectDataContext?.Commit(this, false);
				
				foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					commandsContext.Commit();
				}

				singlePhaseEnlistment.Committed();

				dispose = false;
			}
			catch (Exception e)
			{
				commandsContextsBySqlDatabaseContexts.Values.ForEach(c => ActionUtils.IgnoreExceptions(c.Rollback));

				singlePhaseEnlistment.Aborted(e);
			}
			finally
			{
				if (dispose)
				{
					this.Dispose();
				}
			}
		}

		public virtual void Prepare(PreparingEnlistment preparingEnlistment)
		{
			var dispose = true;

			try
			{
				this.dataAccessObjectDataContext?.Commit(this, false);

				foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					if (commandsContext.SqlDatabaseContext.SupportsPreparedTransactions)
					{
						commandsContext.Prepare();
					}
				}

				preparingEnlistment.Prepared();

				dispose = false;
			}
			catch (TransactionAbortedException)
			{
				throw;
			}
			catch (Exception e)
			{
				commandsContextsBySqlDatabaseContexts.Values.ForEach(c => ActionUtils.IgnoreExceptions(c.Rollback));

				preparingEnlistment.ForceRollback(e);
			}
			finally
			{
				if (dispose)
				{
					this.Dispose();
				}
			}
		}

		public virtual void Rollback(Enlistment enlistment)
		{
			try
			{
				foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					commandsContext.Rollback();
				}
			}
			finally
			{
				this.Dispose();
			}

			enlistment.Done();
		}
	}
}
