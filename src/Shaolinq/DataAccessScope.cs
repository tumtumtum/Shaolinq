// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public partial class DataAccessScope
		: IDisposable
	{
		public DataAccessIsolationLevel IsolationLevel { get; set; }

		private bool complete;
		private readonly DataAccessTransaction transaction;
		private bool completeAsyncCalled;

		private static readonly Func<TransactionContext, CancellationToken, Task> commitAsyncFunc;

		static DataAccessScope()
		{
			var param1 = Expression.Parameter(typeof(TransactionContext));
			var param2 = Expression.Parameter(typeof(CancellationToken));

			commitAsyncFunc = Expression.Lambda<Func<TransactionContext, CancellationToken, Task>>(Expression.Call(param1, "CommitAsync", null), param1, param2).Compile();
		}

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
		
		public void Complete()
		{
			this.complete = true;
		}

		public Task CompleteAsync()
		{
			return this.CompleteAsync(CancellationToken.None);
		}

		public async Task CompleteAsync(CancellationToken cancellationToken)
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

				foreach (var transactionContext in this.transaction.dataAccessModelsByTransactionContext.Keys)
				{
					await commitAsyncFunc(transactionContext, cancellationToken);
				}
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