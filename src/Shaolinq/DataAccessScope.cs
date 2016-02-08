// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shaolinq
{
	public class DataAccessScope
		: IDisposable
	{
		public DataAccessIsolationLevel IsolationLevel { get; set; }

		private bool complete;
		private readonly DataAccessTransaction transaction;
		private bool completeAsyncCalled;

		public DataAccessScope()
			: this(DataAccessIsolationLevel.Unspecified)
		{	
		}

		public void Flush()
		{
			foreach (var dataAccessModel in DataAccessTransaction.Current.ParticipatingDataAccessModels.Where(dataAccessModel => !dataAccessModel.IsDisposed))
			{
				dataAccessModel.Flush();
			}
		}

		public async Task FlushAsync()
		{
			foreach (var dataAccessModel in DataAccessTransaction.Current.ParticipatingDataAccessModels.Where(dataAccessModel => !dataAccessModel.IsDisposed))
			{
				await dataAccessModel.FlushAsync();
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

		public void Complete()
		{
			this.complete = true;
		}

		public async Task CompleteAsync()
		{
			this.completeAsyncCalled = true;

			if (this.transaction == null)
			{
				return;
			}

			if (!this.transaction.HasSystemTransaction)
			{
				if (this.transaction != DataAccessTransaction.Current)
				{
					throw new InvalidOperationException($"Cannot dispose {this.GetType().Name} within another Async/Call context");
				}

				foreach (var transactionContexts in this.transaction.dataAccessModelsByTransactionContext.Keys)
				{
					await transactionContexts.CommitAsync();
				}
			}
		}

		public void Dispose()
		{
			if (this.completeAsyncCalled)
			{
				return;
			}

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