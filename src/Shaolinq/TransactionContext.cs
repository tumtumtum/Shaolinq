// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

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
		public class TransactionExecutionContext
			: IDisposable
		{
			public int Version { get; }
			public event EventHandler Finished;
			public TransactionContext TransactionContext { get; }

			internal TransactionExecutionContext(TransactionContext context)
			{
				this.TransactionContext = context;

				if (context.executionVersionNesting == 0)
				{
					context.executionVersion++;

					this.TransactionContext.dataAccessModel.AsyncLocalExecutionVersion = context.executionVersion;
				}

				this.Version = context.executionVersion;

				context.executionVersionNesting++;
			}

			protected virtual void OnFinished()
			{
				this.Finished?.Invoke(this, EventArgs.Empty);
			}

			public void Dispose()
			{
				this.TransactionContext.executionVersionNesting--;

				if (this.TransactionContext.executionVersionNesting == 0)
				{
					OnFinished();
				}
			}
		}

		private bool disposed;
		private int executionVersion;
		private bool currentlyCommiting;
		private int executionVersionNesting;
		internal SqlDatabaseContext sqlDatabaseContext;
		internal readonly DataAccessModel dataAccessModel;
		private SqlTransactionalCommandsContext commandsContext;
		private DataAccessObjectDataContext dataAccessObjectDataContext;
		internal DataAccessTransaction DataAccessTransaction { get; }
		private readonly Dictionary<string, object> attributes;
		public string DatabaseContextCategoriesKey { get; internal set; }
		
		internal int GetExecutionVersion()
		{
			return this.executionVersion;
		}

		internal bool AnyCommandsHaveBeenPerformed()
		{
			return this.commandsContext != null;
		}

		public static int GetCurrentTransactionContextVersion(DataAccessModel dataAccessModel)
		{
			return dataAccessModel.AsyncLocalExecutionVersion;
		}

		internal static TransactionExecutionContext Acquire(DataAccessModel dataAccessModel, bool forWrite)
		{
			var context = GetOrCreateCurrent(dataAccessModel, forWrite, true);

			if (context == null)
			{
				throw new InvalidOperationException("No Current TransactionContext");
			}

			if (context.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

			var retval = new TransactionExecutionContext(context);

			retval.Finished += context.OnVersionContextFinished;

			return retval;
		}

		public static TransactionContext GetCurrent(DataAccessModel dataAccessModel, bool forWrite)
		{
			return GetOrCreateCurrent(dataAccessModel, forWrite, false);
		}

		private static TransactionContext GetOrCreateCurrent(DataAccessModel dataAccessModel, bool forWrite, bool createTransactionIfNotExist)
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

				context = dataAccessModel.AsyncLocalAmbientTransactionContext;

				if (context == null || context.disposed)
				{
					if (!createTransactionIfNotExist)
					{
						if (context != null)
						{
							dataAccessModel.AsyncLocalAmbientTransactionContext = null;
						}

						return null;
					}

					context = new TransactionContext(null, dataAccessModel);

					dataAccessModel.AsyncLocalAmbientTransactionContext = context;
				}

				if (context.currentlyCommiting)
				{
					throw new InvalidOperationException("The context is currently committing");
				}

				return context;
			}

			dataAccessTransaction.CheckAborted();
			
			if (dataAccessTransaction.systemTransactionCompleted && dataAccessTransaction.systemTransactionStatus == TransactionStatus.Aborted)
			{
				throw new TransactionAbortedException();
			}

			if (dataAccessTransaction.SystemTransaction?.TransactionInformation.Status == TransactionStatus.Aborted)
			{
				throw new TransactionAbortedException();
			}

			var contexts = dataAccessTransaction.transactionContextsByDataAccessModel;

			var skipTest = false;

			if (contexts == null)
			{
				skipTest = true;
				contexts = dataAccessTransaction.transactionContextsByDataAccessModel = new Dictionary<DataAccessModel, TransactionContext>();
			}

			if (skipTest || !contexts.TryGetValue(dataAccessModel, out context))
			{
				context = new TransactionContext(dataAccessTransaction, dataAccessModel);
				contexts[dataAccessModel] = context;

				dataAccessTransaction.AddTransactionContext(context);
			}

			if (context.currentlyCommiting)
			{
				throw new InvalidOperationException("The context is currently committing");
			}

			return context;
		}

		public DataAccessObjectDataContext GetCurrentDataContext()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

			return this.dataAccessObjectDataContext ?? (this.dataAccessObjectDataContext = new DataAccessObjectDataContext(this.dataAccessModel, this.dataAccessModel.GetCurrentSqlDatabaseContext()));
		}
		
		private void OnVersionContextFinished(object sender, EventArgs eventArgs)
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

			if (this.DataAccessTransaction == null)
			{
				this.dataAccessObjectDataContext = null;
				this.commandsContext?.Dispose();
				this.dataAccessModel.AsyncLocalAmbientTransactionContext = null;
				Dispose();
			}
		}

		internal TransactionContext(DataAccessTransaction dataAccessTransaction, DataAccessModel dataAccessModel)
		{
			this.dataAccessModel = dataAccessModel;
			this.DataAccessTransaction = dataAccessTransaction;
			this.DatabaseContextCategoriesKey = "*";
			this.attributes = new Dictionary<string, object>();
			this.executionVersion = dataAccessModel.AsyncLocalExecutionVersion;
		}

		public void SetAttribute(string key, object value)
		{
			this.attributes[key] = value;
		}

		public object GetAttribute(string key)
		{
			if (this.attributes.TryGetValue(key, out var result))
			{
				return result;
			}

			return null;
		}

		internal SqlDatabaseContext GetSqlDatabaseContext()
		{
			return this.dataAccessModel.GetCurrentSqlDatabaseContext();
		}

		[RewriteAsync]
		public virtual SqlTransactionalCommandsContext GetSqlTransactionalCommandsContext()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

			return this.commandsContext ?? (this.commandsContext = GetSqlDatabaseContext().CreateSqlTransactionalCommandsContext(this));
		}

		~TransactionContext()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (this.disposed)
			{
				return;
			}
			
			GC.SuppressFinalize(this);

			try
			{
				this.commandsContext?.Dispose();
				this.DataAccessTransaction?.RemoveTransactionContext(this);
				this.dataAccessObjectDataContext = null;
			}
			finally
			{
				this.disposed = true;
			}
		}

		public virtual void Commit(Enlistment enlistment)
		{
			this.currentlyCommiting = true;

			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(TransactionContext));
			}

			try
			{
				this.commandsContext?.Commit();
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
					Dispose();
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
				Dispose();
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
				if (this.dataAccessObjectDataContext != null)
				{
					this.commandsContext = GetSqlTransactionalCommandsContext();

					this.dataAccessObjectDataContext.Commit(this.commandsContext, false);
					this.commandsContext.Commit();
				}

				singlePhaseEnlistment.Committed();
			}
			catch (Exception e)
			{
				ActionUtils.IgnoreExceptions(() => this.commandsContext.Rollback());

				singlePhaseEnlistment.Aborted(e);
			}
			finally
			{
				Dispose();
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
				if (this.dataAccessObjectDataContext != null)
				{
					this.commandsContext = GetSqlTransactionalCommandsContext();
					this.dataAccessObjectDataContext.Commit(this.commandsContext, false);
					this.commandsContext.Commit();
				}
			}
			catch (Exception e)
			{
				ActionUtils.IgnoreExceptions(() => this.commandsContext?.Rollback());

				throw new DataAccessTransactionAbortedException(e);
			}
			finally
			{
				Dispose();
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
				if (this.dataAccessObjectDataContext != null)
				{
					this.commandsContext = GetSqlTransactionalCommandsContext();
					this.dataAccessObjectDataContext.Commit(this.commandsContext, false);

					if (this.commandsContext.SqlDatabaseContext.SupportsPreparedTransactions)
					{
						this.commandsContext.Prepare();
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
				ActionUtils.IgnoreExceptions(() => this.commandsContext?.Rollback());

				preparingEnlistment.ForceRollback(e);
			}
			finally
			{
				if (dispose)
				{
					Dispose();
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
				ActionUtils.IgnoreExceptions(() => this.commandsContext?.Rollback());
			}
			finally
			{
				Dispose();
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
				ActionUtils.IgnoreExceptions(() => this.commandsContext?.Rollback());
			}
			finally
			{
				Dispose();
			}

			enlistment.Done();
		}
	}
}
