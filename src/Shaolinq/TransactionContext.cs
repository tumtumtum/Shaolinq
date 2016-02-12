// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public partial class TransactionContext
		: ISinglePhaseNotification, IDisposable
	{
		internal static int GetCurrentContextVersion()
		{
			throw new NotImplementedException(MethodBase.GetCurrentMethod().Name);
		}

		private int version;
		private int versionNesting;
		
		internal int GetCurrentVersion()
		{
			return this.version;
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

			public void Dispose()
			{
				this.context.versionNesting--;

				if (this.context.versionNesting == 0)
				{
					this.context.VersionContextFinished(this);
				}
			} 
		}

		private volatile bool disposed;
		internal SqlDatabaseContext sqlDatabaseContext;
		internal readonly DataAccessModel dataAccessModel;
		internal DataAccessTransaction DataAccessTransaction { get; }

		private DataAccessObjectDataContext dataAccessObjectDataContext;
		internal readonly Dictionary<SqlDatabaseContext, SqlTransactionalCommandsContext> commandsContextsBySqlDatabaseContexts;

		internal TransactionContextVersionContext AcquireVersionContext()
		{
			return new TransactionContextVersionContext(this);
		}

		public static TransactionContext GetCurrentContext(DataAccessModel dataAccessModel, bool forWrite)
		{
			TransactionContext context;
			var dataAccessTransaction = DataAccessTransaction.Current;

			if (dataAccessTransaction == null)
			{
				if (forWrite)
				{
					throw new InvalidOperationException("Write operation must be performed inside a scope");
				}

				context = dataAccessModel.asyncLocalTransactionContext.Value;

				if (context == null)
				{
					context = new TransactionContext(null, dataAccessModel);

					dataAccessModel.asyncLocalTransactionContext.Value = context;
				}

				return context;
			}

			if (dataAccessTransaction.HasAborted)
			{
				throw new TransactionAbortedException();
			}

			var contexts = dataAccessModel.transactionContextsByTransaction;

			var skipTest = false;
			
			if (contexts == null)
			{
				skipTest = true;
				contexts = dataAccessModel.transactionContextsByTransaction = new ConcurrentDictionary<DataAccessTransaction, TransactionContext>();
            }

			if (skipTest || !contexts.TryGetValue(dataAccessTransaction, out context))
			{
				context = new TransactionContext(dataAccessTransaction, dataAccessModel);
				contexts[dataAccessTransaction] = context;
				dataAccessModel.asyncLocalTransactionContext.Value = context;

				dataAccessTransaction.AddTransactionContext(context);
			}

			return context;
		}

		public DataAccessObjectDataContext GetCurrentDataContext()
		{
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

		internal void VersionContextFinished(TransactionContextVersionContext versionContext)
		{
			if (this.DataAccessTransaction == null)
			{
				this.dataAccessObjectDataContext = null;

				foreach (var cc in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					cc.Dispose();
				}

				this.commandsContextsBySqlDatabaseContexts.Clear();
			}
		}

		internal TransactionContext(DataAccessTransaction dataAccessTransaction, DataAccessModel dataAccessModel)
		{
			this.dataAccessModel = dataAccessModel;
			this.DataAccessTransaction = dataAccessTransaction;
			this.DatabaseContextCategoriesKey = "*";

			this.commandsContextsBySqlDatabaseContexts = new Dictionary<SqlDatabaseContext, SqlTransactionalCommandsContext>();
		}

		public SqlTransactionalCommandsContext GetCurrentTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext)
		{
			if (this.DataAccessTransaction == null)
			{
				throw new InvalidOperationException("Transaction required");
			}

			return this.AcquirePersistenceTransactionContext(sqlDatabaseContext).SqlDatabaseCommandsContext;
		}

		public virtual DatabaseTransactionContextAcquisition AcquirePersistenceTransactionContext(SqlDatabaseContext sqlDatabaseContext)
		{
			SqlTransactionalCommandsContext commandsContext;

			if (!this.commandsContextsBySqlDatabaseContexts.TryGetValue(sqlDatabaseContext, out commandsContext))
			{
				commandsContext = sqlDatabaseContext.CreateSqlTransactionalCommandsContext(this.DataAccessTransaction);

				this.commandsContextsBySqlDatabaseContexts[sqlDatabaseContext] = commandsContext;
			}

			var retval = new DatabaseTransactionContextAcquisition(this, sqlDatabaseContext, commandsContext);

			if (this.DataAccessTransaction == null)
			{
				retval.Disposed += (s, e) =>
				{
					if (this.GetCurrentVersion() <= 1)
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

			DataAccessTransaction.Current = null;

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
				if (this.DataAccessTransaction != null)
				{
					((IDictionary<DataAccessTransaction, TransactionContext>)dataAccessModel.transactionContextsByTransaction).Remove(this.DataAccessTransaction);
				}

				this.dataAccessModel.asyncLocalTransactionContext.Value = null;
			}
			finally
			{
				disposed = true;

				if (exceptions?.Count > 0)
				{
					throw new AggregateException(exceptions);
				}
			}
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

		internal void Rollback()
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
