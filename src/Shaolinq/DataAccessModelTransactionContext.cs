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
	public class DataAccessModelTransactionContext
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

		internal static IEnumerable<DataAccessModel> GetCurrentlyEnlistedDataAccessModels()
		{
			var dataAccessTransaction = DataAccessTransaction.Current;

			return dataAccessTransaction == null ? Enumerable.Empty<DataAccessModel>() : dataAccessTransaction.dataAccessModels;
		}

		internal class TransactionContextVersionContext
			: IDisposable
		{
			private readonly DataAccessModelTransactionContext context;

			public int Version { get; }

			public TransactionContextVersionContext(DataAccessModelTransactionContext context)
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
		private readonly DataAccessModel dataAccessModel;
		public SqlDatabaseContext SqlDatabaseContext { get; internal set; }
		internal DataAccessTransaction DataAccessTransaction { get; }

		private DataAccessObjectDataContext dataAccessObjectDataContext;
		private readonly Dictionary<SqlDatabaseContext, SqlTransactionalCommandsContext> commandsContextsBySqlDatabaseContexts;

		internal TransactionContextVersionContext AcquireVersionContext()
		{
			return new TransactionContextVersionContext(this);
		}

		public static DataAccessModelTransactionContext GetCurrentContext(DataAccessModel dataAccessModel, bool forWrite)
		{
			DataAccessModelTransactionContext context;
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
					context = new DataAccessModelTransactionContext(null, dataAccessModel);

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
				contexts = dataAccessModel.transactionContextsByTransaction = new ConcurrentDictionary<DataAccessTransaction, DataAccessModelTransactionContext>();
            }

			if (skipTest || !contexts.TryGetValue(dataAccessTransaction, out context))
			{
				context = new DataAccessModelTransactionContext(dataAccessTransaction, dataAccessModel);
				contexts[dataAccessTransaction] = context;
				dataAccessModel.asyncLocalTransactionContext.Value = context;

				dataAccessTransaction.dataAccessModels.Add(dataAccessModel);

				dataAccessTransaction.SystemTransaction?.EnlistVolatile(context, EnlistmentOptions.None);
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

		internal void VersionContextFinished(TransactionContextVersionContext versionContext)
		{
			if (this.DataAccessTransaction == null)
			{
				this.dataAccessObjectDataContext = null;
			}
		}

		internal DataAccessModelTransactionContext(DataAccessTransaction dataAccessTransaction, DataAccessModel dataAccessModel)
		{
			this.dataAccessModel = dataAccessModel;
			this.DataAccessTransaction = dataAccessTransaction;
			this.DatabaseContextCategoriesKey = ".";

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
			SqlTransactionalCommandsContext retval;

			if (this.DataAccessTransaction == null)
			{
				retval = sqlDatabaseContext.CreateSqlTransactionalCommandsContext(null);

				return new DatabaseTransactionContextAcquisition(this, sqlDatabaseContext, retval);
			}

			if (!this.commandsContextsBySqlDatabaseContexts.TryGetValue(sqlDatabaseContext, out retval))
			{
				retval = sqlDatabaseContext.CreateSqlTransactionalCommandsContext(this.DataAccessTransaction);

				this.commandsContextsBySqlDatabaseContexts[sqlDatabaseContext] = retval;
			}

			return new DatabaseTransactionContextAcquisition(this, sqlDatabaseContext, retval);
		}

		~DataAccessModelTransactionContext()
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

			if (this.DataAccessTransaction != null)
			{
				((IDictionary<DataAccessTransaction, DataAccessModelTransactionContext>)dataAccessModel.transactionContextsByTransaction).Remove(this.DataAccessTransaction);
				this.DataAccessTransaction.dataAccessModels.Add(dataAccessModel);
			}

			DataAccessTransaction.Current = null;

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

			this.dataAccessModel.asyncLocalTransactionContext.Value = null;
			
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

		public void Commit()
		{
			try
			{
				this.dataAccessObjectDataContext?.Commit(this, false);

				foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					commandsContext.Commit();
				}
			}
			catch (Exception e)
			{
				commandsContextsBySqlDatabaseContexts.Values.ForEach(c => ActionUtils.IgnoreExceptions(c.Rollback));
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
