// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public partial class DataAccessScope
		: IDisposable
	{
		public DataAccessIsolationLevel IsolationLevel { get; set; }

		private bool complete;
		private readonly DataAccessTransaction transaction;
		
		public DataAccessScope()
			: this(DataAccessIsolationLevel.Unspecified)
		{
			if (DataAccessTransaction.Current == null)
			{
				transaction = DataAccessTransaction.Current = new DataAccessTransaction();
			}
		}

		[RewriteAsync]
		public void Flush()
		{
			foreach (var dataAccessModel in DataAccessTransaction.Current.ParticipatingDataAccessModels)
			{
				if (!dataAccessModel.IsDisposed)
				{
					dataAccessModel.Flush();
				}
			}
		}

		public DataAccessScope(DataAccessIsolationLevel isolationLevel)
		{
			this.IsolationLevel = isolationLevel;

			if (DataAccessTransaction.Current == null)
			{
				this.transaction = new DataAccessTransaction();

				DataAccessTransaction.Current = this.transaction;
			}
		}
		
		[RewriteAsync]
		public void Complete()
		{
			this.complete = true;
			
			if (this.transaction == null)
			{
				return;
			}

			if (this.transaction.HasSystemTransaction)
			{
				return;
			}

			if (this.transaction != DataAccessTransaction.Current)
			{
				throw new InvalidOperationException($"Cannot dispose {this.GetType().Name} within another Async/Call context");
			}

			foreach (var transactionContext in this.transaction.dataAccessModelsByTransactionContext.Keys)
			{
				transactionContext.Commit();
			}

			if (DataAccessTransaction.Current != null)
			{
				DataAccessTransaction.Current.Dispose();
			}
		}

		public void Dispose()
		{
			if (!this.complete)
			{
				if (this.transaction == null)
				{
					throw new DataAccessTransactionAbortedException();
				}
				else
				{
					foreach (var transactionContext in this.transaction.dataAccessModelsByTransactionContext.Keys)
					{
						transactionContext.Rollback();
						transactionContext.Dispose();
					}
				}
			}
			else
			{
				if (this.transaction != null)
				{
					foreach (var transactionContext in this.transaction.dataAccessModelsByTransactionContext.Keys)
					{
						transactionContext.Dispose();
					}
				}
			}
		}
	}
}