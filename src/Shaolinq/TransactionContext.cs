// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Transactions;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class TransactionContext
		: ISinglePhaseNotification, IDisposable
	{
		public Transaction Transaction { get; }
		private readonly DataAccessModel dataAccessModel;
		public SqlDatabaseContext SqlDatabaseContext { get; internal set; }

		private DataAccessObjectDataContext dataAccessObjectDataContext;
		private readonly Dictionary<SqlDatabaseContext, SqlTransactionalCommandsContext> commandsContextsBySqlDatabaseContexts;
		
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

				var rootContexts = dataAccessModel.rootTransactionContexts;

                if (rootContexts == null)
				{
					rootContexts = dataAccessModel.rootTransactionContexts = new ConcurrentDictionary<Thread, TransactionContext>();
				}

				if (!rootContexts.TryGetValue(Thread.CurrentThread, out context))
				{
					context = new TransactionContext(null, dataAccessModel);

					rootContexts[Thread.CurrentThread] = context;
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

				transaction.TransactionCompleted += (o, e) =>
				{
					((IDictionary<Transaction, TransactionContext>)contexts).Remove(transaction);
				};

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
			else
			{
				if (!this.commandsContextsBySqlDatabaseContexts.TryGetValue(sqlDatabaseContext, out retval))
				{
					retval = sqlDatabaseContext.CreateSqlTransactionalCommandsContext(this.Transaction);

					this.commandsContextsBySqlDatabaseContexts[sqlDatabaseContext] = retval;
				}

				return new DatabaseTransactionContextAcquisition(this, sqlDatabaseContext, retval);
			}
		}

		public static void FlushConnections(DataAccessModel dataAccessModel)
		{
			foreach (var commandsContext in dataAccessModel
				.rootTransactionContexts
				.Values
				.SelectMany(context => context.commandsContextsBySqlDatabaseContexts.Values))
			{
				try
				{
					commandsContext.Dispose();
				}
				catch
				{
				}
			}
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

		private bool disposed;

		protected void Dispose(bool disposing)
		{
			if (disposed)
			{
				return;
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
				// Don't properly support two-phase-commits yet
				// This could possibly still throw and fail

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
			enlistment.Done();
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

		public virtual void Prepare(PreparingEnlistment preparingEnlistment)
		{
			try
			{
				preparingEnlistment.Prepared();
			}
			catch (TransactionAbortedException)
			{
				throw;
			}
			catch (Exception e)
			{
				commandsContextsBySqlDatabaseContexts.Values.ForEach(c => ActionUtils.IgnoreExceptions(c.Rollback));

				preparingEnlistment.ForceRollback(e);

				this.Dispose();
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
