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
		}
		
		[RewriteAsync]
		public void Flush()
		{
			foreach (var dataAccessModel in DataAccessTransaction.Current.ParticipatingDataAccessModels.Where(dataAccessModel => !dataAccessModel.IsDisposed))
			{
				dataAccessModel.Flush();
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
			}

			if (this.transaction != null && !this.transaction.HasSystemTransaction)
			{
				if (this.transaction != DataAccessTransaction.Current)
				{
					throw new InvalidOperationException($"Cannot dispose {this.GetType().Name} within another Async/Call context");
				}

				foreach (var transactionContexts in this.transaction.dataAccessModelsByTransactionContext.Keys)
				{
					transactionContexts.Commit();
				}
			}
		}
	}
}