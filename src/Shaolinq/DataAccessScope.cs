// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public enum DataAccessScopeOptions
	{
		Required,
		RequiresNew,
		Suppress
	}

	public partial class DataAccessScope
		: IDisposable
	{
		public DataAccessIsolationLevel IsolationLevel { get; set; }

		private bool complete;
		private bool disposed;
		private readonly DataAccessScopeOptions options;
		private readonly DataAccessTransaction transaction;
		private readonly DataAccessTransaction savedTransaction;

		public static DataAccessScope CreateReadCommitted()
        {
            return CreateReadCommitted(TimeSpan.Zero);
        }

        public static DataAccessScope CreateRepeatableRead()
        {
            return CreateRepeatableRead(TimeSpan.Zero);
        }

        public static DataAccessScope CreateReadUncommited()
        {
            return CreateReadUncommited(TimeSpan.Zero);
        }

        public static DataAccessScope CreateSerializable()
        {
            return CreateSerializable(TimeSpan.Zero);
        }

        public static DataAccessScope CreateSnapshot()
        {
            return CreateSnapshot(TimeSpan.Zero);
        }

        public static DataAccessScope CreateChaos()
        {
            return CreateChaos(TimeSpan.Zero);
        }

        public static DataAccessScope CreateReadCommitted(TimeSpan timeout)
	    {
	        return new DataAccessScope(DataAccessIsolationLevel.ReadCommitted);
	    }

        public static DataAccessScope CreateRepeatableRead(TimeSpan timeout)
        {
            return new DataAccessScope(DataAccessIsolationLevel.RepeatableRead);
        }

        public static DataAccessScope CreateReadUncommited(TimeSpan timeout)
        {
            return new DataAccessScope(DataAccessIsolationLevel.ReadUncommitted);
        }

        public static DataAccessScope CreateSerializable(TimeSpan timeout)
        {
            return new DataAccessScope(DataAccessIsolationLevel.Serializable);
        }

        public static DataAccessScope CreateSnapshot(TimeSpan timeout)
        {
            return new DataAccessScope(DataAccessIsolationLevel.Snapshot);
        }

        public static DataAccessScope CreateChaos(TimeSpan timeout)
        {
            return new DataAccessScope(DataAccessIsolationLevel.Chaos);
        }
        
        public DataAccessScope()
			: this(DataAccessIsolationLevel.Unspecified)
		{
		}

	    public DataAccessScope(DataAccessIsolationLevel isolationLevel)
            : this(isolationLevel, TimeSpan.Zero)
	    {
	    }

		public DataAccessScope(DataAccessIsolationLevel isolationLevel, TimeSpan timeout)
			: this(isolationLevel, DataAccessScopeOptions.Required, timeout)
		{
		}

		public DataAccessScope(DataAccessIsolationLevel isolationLevel, DataAccessScopeOptions options, TimeSpan timeout)
		{
			this.IsolationLevel = isolationLevel;

			switch (options)
			{
			case DataAccessScopeOptions.Required:
				this.savedTransaction = DataAccessTransaction.Current;
				if (this.savedTransaction == null)
				{
					this.transaction = new DataAccessTransaction(isolationLevel) { timeout = timeout };

					DataAccessTransaction.Current = this.transaction;
				}
				break;
			case DataAccessScopeOptions.RequiresNew:
				this.savedTransaction = DataAccessTransaction.Current;
				if (this.savedTransaction == null)
				{
					this.transaction = new DataAccessTransaction(isolationLevel) { timeout = timeout };

					this.options = options;
					DataAccessTransaction.Current = this.transaction;
				}
				break;
			case DataAccessScopeOptions.Suppress:
				this.savedTransaction = DataAccessTransaction.Current;
				if (this.savedTransaction != null)
				{
					this.options = options;
					DataAccessTransaction.Current = null;
				}
				break;
			}

			if (DataAccessTransaction.Current == null)
			{
			    this.transaction = new DataAccessTransaction(isolationLevel) { timeout = timeout };

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
				if (this.options == DataAccessScopeOptions.Suppress)
				{
					DataAccessTransaction.Current = this.savedTransaction;
				}

				return;
			}

			if (this.transaction.HasSystemTransaction)
			{
				return;
			}

			try
			{
				if (this.transaction != DataAccessTransaction.Current)
				{
					throw new InvalidOperationException($"Cannot commit {this.GetType().Name} within another Async/Call context");
				}

				this.transaction.Commit();
				this.transaction.Dispose();
			}
			finally
			{
				DataAccessTransaction.Current = this.savedTransaction;
			}
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
			if (disposed)
			{
				throw new ObjectDisposedException(nameof(DataAccessScope));
			}

			disposed = true;

			if (this.complete)
			{
				this.transaction?.Dispose();
			}
			else
			{
				if (this.transaction == null)
				{
					throw new DataAccessTransactionAbortedException();
				}

				this.transaction.Rollback();
			}
		}
	}
}