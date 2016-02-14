// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
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
				this.transaction = new DataAccessTransaction(isolationLevel);

				DataAccessTransaction.Current = this.transaction;
			}
		}

		[RewriteAsync]
		public void Flush()
		{
			Save();
		}

		[RewriteAsync]
		public void Flush(DataAccessModel dataAccessModel)
		{
			Save(dataAccessModel);
		}

		[RewriteAsync]
		public void Save()
		{
			foreach (var dataAccessModel in DataAccessTransaction.Current.ParticipatingDataAccessModels)
			{
				if (!dataAccessModel.IsDisposed)
				{
					dataAccessModel.Flush();
				}
			}
		}

		[RewriteAsync]
		public void Save(DataAccessModel dataAccessModel)
		{
			if (!dataAccessModel.IsDisposed)
			{
				dataAccessModel.Flush();
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

			this.transaction.Commit();
			this.transaction.Dispose();
		}

		[RewriteAsync]
		public void Fail()
		{
			this.complete = false;

			if (this.transaction != null)
			{
				this.transaction.Rollback();
				this.transaction.Dispose();
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
					this.transaction.Rollback();
				}
			}
			else
			{
				if (this.transaction != null)
				{
					transaction.Dispose();

					this.transaction.Dispose();
				}
			}
		}
	}
}