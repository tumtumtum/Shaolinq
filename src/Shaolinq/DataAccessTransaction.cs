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

				if (value != null)
				{
					if (!value.disposed && value.SystemTransaction == Transaction.Current)
					{
						return value;
					}

					current.Value = null;
				}

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
		internal DataAccessScope scope;
		internal Transaction SystemTransaction { get; set; }
		internal bool HasSystemTransaction => this.SystemTransaction != null;
		internal Dictionary<DataAccessModel, TransactionContext> dataAccessModelsByTransactionContext;
		internal bool aborted;

		public DataAccessIsolationLevel IsolationLevel { get; private set; }
		public IEnumerable<DataAccessModel> ParticipatingDataAccessModels => this.dataAccessModelsByTransactionContext?.Keys ?? Enumerable.Empty<DataAccessModel>();

		internal DataAccessTransaction()
			: this(DataAccessIsolationLevel.Unspecified)
		{
		}

		internal DataAccessTransaction(DataAccessIsolationLevel isolationLevel)
		{
			this.IsolationLevel = isolationLevel;
			this.SystemTransaction = Transaction.Current;
		}

		internal DataAccessTransaction(DataAccessIsolationLevel isolationLevel, DataAccessScope scope, TimeSpan timeout)
		{
			this.timeout = timeout;
			this.scope = scope;
			this.IsolationLevel = isolationLevel;
			this.SystemTransaction = Transaction.Current;
		}

		public void AddTransactionContext(TransactionContext context)
		{
			if (dataAccessModelsByTransactionContext == null)
			{
				dataAccessModelsByTransactionContext = new Dictionary<DataAccessModel, TransactionContext>();
			}

			this.dataAccessModelsByTransactionContext[context.dataAccessModel] = context;

			this.SystemTransaction?.EnlistVolatile(context, EnlistmentOptions.None);
		}

		public void RemoveTransactionContext(TransactionContext context)
		{
			if (!this.isfinishing)
			{
				this.dataAccessModelsByTransactionContext?.Remove(context.dataAccessModel);
			}
		}

		[RewriteAsync]
		public void Commit()
		{
			this.isfinishing = true;

			if (this.dataAccessModelsByTransactionContext != null)
			{
				foreach (var transactionContext in this.dataAccessModelsByTransactionContext.Values)
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

			if (this.dataAccessModelsByTransactionContext != null)
			{
				foreach (var transactionContext in this.dataAccessModelsByTransactionContext.Values)
				{
					ActionUtils.IgnoreExceptions(() => transactionContext.Rollback());
				}
			}
		}

		public void Dispose()
		{
			this.isfinishing = true;

			if (this.dataAccessModelsByTransactionContext != null)
			{
				foreach (var transactionContext in this.dataAccessModelsByTransactionContext.Values)
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