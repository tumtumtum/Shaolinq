// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;

namespace Shaolinq
{
	public class DataAccessModelTransactionManager
		: IDisposable
	{
		[ThreadStatic]
		internal static TransactionContext currentlyCommitingTransactionContext;

		[ThreadStatic]
		private static Dictionary<DataAccessModel, DataAccessModelTransactionManager> ambientTransactionManagers;

		public DataAccessModel DataAccessModel { get; }
		
        
		/// <summary>
		/// Gets the <see cref="DataAccessModelTransactionManager "/> for the current <see cref="Shaolinq.DataAccessModel"/> for the current thread.
		/// </summary>
		/// <remarks>
		/// The framework does not support accessing objects created within different transactions
		/// from other transactions. For each Transaction there may be more than one attached
		/// <see cref="TransactionContext"/> (one for each thread that participates in the transaction).
		/// </remarks>
		public static DataAccessModelTransactionManager GetAmbientTransactionManager(DataAccessModel dataAccessModel)
		{
			DataAccessModelTransactionManager retval;
			
			if (ambientTransactionManagers == null)
			{
				ambientTransactionManagers = new Dictionary<DataAccessModel, DataAccessModelTransactionManager>();
			}

			var transactionManagers = ambientTransactionManagers;

			if (transactionManagers.TryGetValue(dataAccessModel, out retval))
			{
				return retval;
			}

			retval = new DataAccessModelTransactionManager(dataAccessModel);

			transactionManagers[dataAccessModel] = retval;

			return retval;
		}

		~DataAccessModelTransactionManager()
		{
			this.Dispose();
		}
        
		public void FlushConnections()
		{
			this.rootContext?.FlushConnections();
		}

		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref this.disposed, 1, 0) != 0)
			{
				return;
			}

			this.rootContext?.Dispose();

			GC.SuppressFinalize(this);
		}

		private int disposed;
		private TransactionContext rootContext;
		private readonly IDictionary<Transaction, TransactionContext> transactionContextsByTransaction;

		public DataAccessModelTransactionManager(DataAccessModel dataAccessModel)
		{
			EventHandler[] handler = { null };
			var weakThis = new WeakReference(this);

			this.DataAccessModel = dataAccessModel;
			this.transactionContextsByTransaction = new Dictionary<Transaction, TransactionContext>();
            
			handler[0] = delegate(object sender, EventArgs eventArgs)
			{
				var strongThis = (DataAccessModelTransactionManager)weakThis.Target;

				if (strongThis != null)
				{
					strongThis.Dispose();
				}
				else
				{
					((DataAccessModel)sender).Disposed -= handler[0];
				}
			};

			this.DataAccessModel.Disposed += handler[0];
		}

		public virtual TransactionContext GetCurrentContext(bool forWrite)
		{
			TransactionContext retval;

			if (!this.TryGetCurrentContext(forWrite, out retval))
			{
				throw new NotSupportedException("Write operation must be performed inside a transaction context");
			}

			return retval;
		}

		public virtual bool TryGetCurrentContext(bool forWrite, out TransactionContext retval)
		{
			Transaction transaction;
			
			if (currentlyCommitingTransactionContext != null)
			{
				retval = currentlyCommitingTransactionContext;

				return true;
			}
			try

			{
				transaction = Transaction.Current;
			}
			catch (InvalidOperationException)
			{
				transaction = Transaction.Current = null;
			}

			if (transaction == null && forWrite)
			{
				retval = null;

				return false;
			}

			if (transaction != null)
			{
				if (transaction.TransactionInformation.Status == TransactionStatus.Aborted)
				{
					throw new TransactionAbortedException();
				}

				if (this.transactionContextsByTransaction.TryGetValue(transaction, out retval))
				{
					return true;
				}

				retval = new TransactionContext(this.DataAccessModel, transaction);
                    
				transaction.TransactionCompleted += delegate
				{
					this.transactionContextsByTransaction.Remove(transaction);
				};

				transaction.EnlistVolatile(retval, EnlistmentOptions.None);

				this.transactionContextsByTransaction[transaction] = retval;
			}
			else
			{
				if (this.rootContext == null)
				{
					retval = new TransactionContext(this.DataAccessModel, null);

					this.rootContext = retval;
				}
				else
				{
					retval = this.rootContext;
				}
			}

			return true;
		}
	}
}
