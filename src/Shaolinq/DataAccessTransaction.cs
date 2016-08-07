// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public partial class DataAccessTransaction
		: IDisposable
	{
		private static readonly AsyncLocal<DataAccessTransaction> current = new AsyncLocal<DataAccessTransaction>();

		public static DataAccessTransaction Current
		{
			get
			{
				var value = current.Value;

				if (value == null)
				{
					return null;
				}

				if (!value.disposed)
				{
					try
					{
						if (value.SystemTransaction == Transaction.Current || (value.SystemTransaction != null && value.SystemTransaction.TransactionInformation.Status != TransactionStatus.Aborted && Transaction.Current == null))
						{
							return value;
						}
					}
					catch (ObjectDisposedException)
					{
					}
				}

				current.Value = current.Value.previousTransaction;

				return null;
			}

			internal set
			{
				current.Value = value;
			}
		}

		private bool disposed;
		private bool isfinishing;

		internal TimeSpan timeout;
		internal Transaction SystemTransaction { get; set; }
		internal bool HasSystemTransaction => this.SystemTransaction != null;
		internal Dictionary<DataAccessModel, TransactionContext> transactionContextsByDataAccessModel;
		internal bool aborted;
		private readonly DataAccessTransaction previousTransaction;
		internal bool systemTransactionCompleted;
		internal TransactionStatus systemTransactionStatus;

		public DataAccessIsolationLevel IsolationLevel { get; private set; }
		public IEnumerable<DataAccessModel> ParticipatingDataAccessModels => this.transactionContextsByDataAccessModel?.Keys ?? Enumerable.Empty<DataAccessModel>();

		internal DataAccessTransaction()
			: this(DataAccessIsolationLevel.Unspecified)
		{
		}

		internal DataAccessTransaction(DataAccessIsolationLevel isolationLevel)
			: this(isolationLevel, TimeSpan.Zero)
		{
		}

		internal DataAccessTransaction(DataAccessIsolationLevel isolationLevel, TimeSpan timeout)
		{
			this.timeout = timeout;
			this.IsolationLevel = isolationLevel;
			this.SystemTransaction = Transaction.Current;

			if (this.SystemTransaction != null)
			{
				this.previousTransaction = DataAccessTransaction.Current;

				this.SystemTransaction.TransactionCompleted += (sender, eventArgs) =>
				{
					this.systemTransactionCompleted = true;
					this.systemTransactionStatus = eventArgs.Transaction.TransactionInformation.Status;

					this.Dispose();
				};
			}
		}

		public void AddTransactionContext(TransactionContext context)
		{
			if (this.transactionContextsByDataAccessModel == null)
			{
				this.transactionContextsByDataAccessModel = new Dictionary<DataAccessModel, TransactionContext>();
			}

			this.transactionContextsByDataAccessModel[context.dataAccessModel] = context;

			this.SystemTransaction?.EnlistVolatile(context, EnlistmentOptions.None);
		}

		public void RemoveTransactionContext(TransactionContext context)
		{
			if (!this.isfinishing)
			{
				this.transactionContextsByDataAccessModel?.Remove(context.dataAccessModel);
			}
		}

		[RewriteAsync]
		public void Commit()
		{
			this.isfinishing = true;

			if (this.transactionContextsByDataAccessModel != null)
			{
				foreach (var transactionContext in this.transactionContextsByDataAccessModel.Values)
				{
					transactionContext.Commit();
					transactionContext.Dispose();
				}
			}
		}

		[RewriteAsync]
		public void Rollback()
		{
			this.isfinishing = true;
			this.aborted = true;

			if (this.transactionContextsByDataAccessModel != null)
			{
				foreach (var transactionContext in this.transactionContextsByDataAccessModel.Values)
				{
					ActionUtils.IgnoreExceptions(() => transactionContext.Rollback());
				}
			}
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			this.isfinishing = true;

			if (this.transactionContextsByDataAccessModel != null)
			{
				foreach (var transactionContext in this.transactionContextsByDataAccessModel.Values)
				{
					ActionUtils.IgnoreExceptions(() => transactionContext.Rollback());
				}
			}

			this.disposed = true;
		}

		internal void CheckAborted()
		{
			if (this.aborted)
			{
				throw new TransactionAbortedException();
			}
		}
	}
}