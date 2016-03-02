// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Transactions;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public partial class TransactionContext
		: ISinglePhaseNotification, IDisposable
	{
		public class TransactionContextExecutionVersionContext
			: IDisposable
		{
			private readonly TransactionContext context;

			public int Version { get; }

			public TransactionContextExecutionVersionContext(TransactionContext context)
			{
				this.context = context;

				if (context.executionVersionNesting == 0)
				{
					context.executionVersion++;

					this.context.dataAccessModel.AsyncLocalExecutionVersion = context.executionVersion;
				}

				this.Version = context.executionVersion;

				context.executionVersionNesting++;
			}

			public void Dispose()
			{
				this.context.executionVersionNesting--;

				if (this.context.executionVersionNesting == 0)
				{
					this.context.VersionContextFinished(this);
				}
			} 
		}

		private int executionVersion;
		private int executionVersionNesting;
		private volatile bool disposed;
		internal SqlDatabaseContext sqlDatabaseContext;
		internal readonly DataAccessModel dataAccessModel;
		internal DataAccessTransaction DataAccessTransaction { get; }
		private DataAccessObjectDataContext dataAccessObjectDataContext;
		internal readonly Dictionary<SqlDatabaseContext, SqlTransactionalCommandsContext> commandsContextsBySqlDatabaseContexts;

		internal int GetExecutionVersion()
		{
			return this.executionVersion;
		}

		public static int GetCurrentTransactionContextVersion(DataAccessModel dataAccessModel)
		{
			return dataAccessModel.AsyncLocalExecutionVersion;
		}

		internal TransactionContextExecutionVersionContext AcquireVersionContext()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

			return new TransactionContextExecutionVersionContext(this);
		}

		public static TransactionContext GetCurrentContext(DataAccessModel dataAccessModel, bool forWrite)
		{
			TransactionContext context;
			var dataAccessTransaction = DataAccessTransaction.Current;
			
			if (dataAccessTransaction == null && Transaction.Current != null)
			{
				dataAccessTransaction = DataAccessTransaction.Current = new DataAccessTransaction(DataAccessIsolationLevel.Unspecified);
			}

			if (dataAccessTransaction == null)
			{
				if (forWrite)
				{
					throw new InvalidOperationException("Write operation must be performed inside a scope");
				}

				context = dataAccessModel.AsyncLocalTransactionContext;

				if (context == null || context.disposed)
				{
					context = new TransactionContext(null, dataAccessModel);

					dataAccessModel.AsyncLocalTransactionContext = context;
				}

				return context;
			}

			if (dataAccessTransaction.SystemTransaction?.TransactionInformation.Status == TransactionStatus.Aborted)
			{
				throw new TransactionAbortedException();
			}

			var contexts = dataAccessTransaction.dataAccessModelsByTransactionContext;

			var skipTest = false;
			
			if (contexts == null)
			{
				skipTest = true;
				contexts = dataAccessTransaction.dataAccessModelsByTransactionContext = new Dictionary<DataAccessModel, TransactionContext>();
			}

			if (skipTest || !contexts.TryGetValue(dataAccessModel, out context))
			{
				context = new TransactionContext(dataAccessTransaction, dataAccessModel);
				contexts[dataAccessModel] = context;
				dataAccessModel.AsyncLocalTransactionContext = context;

				dataAccessTransaction.AddTransactionContext(context);
			}

			return context;
		}

		public DataAccessObjectDataContext GetCurrentDataContext()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

			if (dataAccessObjectDataContext == null)
			{
				dataAccessObjectDataContext = new DataAccessObjectDataContext(this.dataAccessModel, this.dataAccessModel.GetCurrentSqlDatabaseContext(), false);
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
					this.DatabaseContextCategoriesKey = "*";
				}
				else
				{
					this.DatabaseContextCategoriesKey = string.Join(",", value);
				}
			}
		}
		private string[] databaseContextCategories;

		public string DatabaseContextCategoriesKey { get; private set; }

		internal void VersionContextFinished(TransactionContextExecutionVersionContext executionVersionContext)
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

			if (this.DataAccessTransaction == null)
			{
				this.dataAccessObjectDataContext = null;

				foreach (var cc in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					cc.Dispose();
				}

				this.commandsContextsBySqlDatabaseContexts.Clear();
				this.dataAccessModel.AsyncLocalTransactionContext = null;
				this.Dispose();
			}
		}

		internal TransactionContext(DataAccessTransaction dataAccessTransaction, DataAccessModel dataAccessModel)
		{
			this.dataAccessModel = dataAccessModel;
			this.DataAccessTransaction = dataAccessTransaction;
			this.DatabaseContextCategoriesKey = "*";
			this.executionVersion = dataAccessModel.AsyncLocalExecutionVersion;

			this.commandsContextsBySqlDatabaseContexts = new Dictionary<SqlDatabaseContext, SqlTransactionalCommandsContext>();
		}

		public SqlTransactionalCommandsContext GetCurrentTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext)
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

			if (this.DataAccessTransaction == null)
			{
				throw new InvalidOperationException("Transaction required");
			}

			return this.AcquirePersistenceTransactionContext(sqlDatabaseContext).SqlDatabaseCommandsContext;
		}

		public virtual DatabaseTransactionContextAcquisition AcquirePersistenceTransactionContext(SqlDatabaseContext sqlDatabaseContext)
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

			SqlTransactionalCommandsContext commandsContext;

			if (!this.commandsContextsBySqlDatabaseContexts.TryGetValue(sqlDatabaseContext, out commandsContext))
			{
				commandsContext = sqlDatabaseContext.CreateSqlTransactionalCommandsContext(this.DataAccessTransaction);
                
				this.commandsContextsBySqlDatabaseContexts[sqlDatabaseContext] = commandsContext;
			}

			var startIndex = this.GetExecutionVersion();

			var retval = new DatabaseTransactionContextAcquisition(this, sqlDatabaseContext, commandsContext);

			if (this.DataAccessTransaction == null)
			{
				retval.Disposed += (s, e) =>
				{
					if (this.GetExecutionVersion() <= startIndex)
					{
						this.dataAccessObjectDataContext = null;

						foreach (var cc in this.commandsContextsBySqlDatabaseContexts.Values)
						{
							cc.Dispose();
						}

						this.commandsContextsBySqlDatabaseContexts.Clear();
					}
				};
			}

			return retval;
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

			List<Exception> exceptions = null;

			foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
			{
				try
				{
					commandsContext.Dispose();
				}
				catch (Exception e)
				{
					if (exceptions == null)
					{
						exceptions = new List<Exception>();
					}

					exceptions.Add(e);
				}
			}

			try
			{
				this.DataAccessTransaction?.RemoveTransactionContext(this);

				this.dataAccessModel.AsyncLocalTransactionContext = null;
			}
			finally
			{
				disposed = true;

				if (exceptions?.Count > 0)
				{
					throw new AggregateException(exceptions);
				}
			}

			if (this.DataAccessTransaction != null)
			{
				if (!DataAccessTransaction.HasSystemTransaction)
				{
					DataAccessTransaction.Current?.Dispose();
				}
			}
		}

		public virtual void Commit(Enlistment enlistment)
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

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
			if (this.disposed)
			{
				return;
			}

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
			if (this.disposed)
			{
				return;
			}

			try
			{
				this.dataAccessObjectDataContext?.Commit(this, false);
				
				foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					commandsContext.Commit();
				}

				singlePhaseEnlistment.Committed();
			}
			catch (Exception e)
			{
				commandsContextsBySqlDatabaseContexts.Values.ForEach(c => ActionUtils.IgnoreExceptions(c.Rollback));

				singlePhaseEnlistment.Aborted(e);
			}
			finally
			{
				this.Dispose();
			}
		}

		[RewriteAsync]
		public void Commit()
		{
			if (this.disposed)
			{
				return;
			}

			try
			{
				// ReSharper disable once UseNullPropagation
				if (this.dataAccessObjectDataContext != null)
				{
					this.dataAccessObjectDataContext.Commit(this, false);
				}

				foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					commandsContext.Commit();
				}
			}
			catch (Exception e)
			{
				commandsContextsBySqlDatabaseContexts.Values.ForEach(c => ActionUtils.IgnoreExceptions(c.Rollback));

				throw new DataAccessTransactionAbortedException(e);
			}
			finally
			{
				this.Dispose();
			}
		}
		
		public virtual void Prepare(PreparingEnlistment preparingEnlistment)
		{
			if (this.disposed)
			{
				return;
			}

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

		[RewriteAsync]
		internal void Rollback()
		{
			if (this.disposed)
			{
				return;
			}

			try
			{
				foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					ActionUtils.IgnoreExceptions(() => commandsContext.Rollback());
				}
			}
			finally
			{
				this.Dispose();
			}
		}

		public virtual void Rollback(Enlistment enlistment)
		{
			if (this.disposed)
			{
				return;
			}

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
