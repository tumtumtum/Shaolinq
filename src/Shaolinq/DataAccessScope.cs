// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public partial class DataAccessScope
		: IDisposable
	{
		public DataAccessIsolationLevel IsolationLevel { get; set; }

		private bool complete;
		private bool disposed;
		private readonly bool isRoot;
		private readonly DataAccessScopeOptions options;
		
		private readonly DataAccessTransaction transaction;
		private readonly DataAccessScope outerScope;

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
			var currentTransaction = DataAccessTransaction.Current;

			this.options = options;

			switch (options)
			{
			case DataAccessScopeOptions.Required:
				if (currentTransaction == null)
				{
					this.isRoot = true;
					this.transaction = new DataAccessTransaction(isolationLevel, this, timeout);
					DataAccessTransaction.Current = this.transaction;
				}
				else
				{
					this.transaction = currentTransaction;
					this.outerScope = currentTransaction.scope;
					currentTransaction.scope = this;
				}
				break;
			case DataAccessScopeOptions.RequiresNew:
				this.isRoot = true;
				this.outerScope = currentTransaction?.scope;
				this.transaction = new DataAccessTransaction(isolationLevel, this, timeout);
				DataAccessTransaction.Current = this.transaction;
				break;
			case DataAccessScopeOptions.Suppress:
				if (currentTransaction != null)
				{
					this.outerScope = currentTransaction.scope;
					DataAccessTransaction.Current = null;
				}
				break;
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
			this.transaction.CheckAborted();

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
			this.transaction.CheckAborted();

			if (!dataAccessModel.IsDisposed)
			{
				dataAccessModel.Flush();
			}
		}
		
		[RewriteAsync]
		public void Complete()
		{
			this.complete = true;

			this.transaction.CheckAborted();

			if (this.transaction == null)
			{
				if (this.options == DataAccessScopeOptions.Suppress)
				{
					DataAccessTransaction.Current = this.outerScope.transaction;
					DataAccessTransaction.Current.scope = this.outerScope;
				}

				return;
			}

			if (!this.isRoot)
			{
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
				if (this.outerScope != null)
				{
					this.outerScope.transaction.scope = this.outerScope;
					DataAccessTransaction.Current = this.outerScope.transaction;
				}
				else
				{
					DataAccessTransaction.Current = null;
				}
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

		public SqlTransactionalCommandsContext GetCurrentSqlDataTransactionContext(DataAccessModel model)
		{
			return model.GetCurrentSqlDatabaseTransactionContext();
		}
		
		public void Dispose()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(DataAccessScope));
			}

			this.disposed = true;
			
			if (!this.complete)
			{
				this.transaction?.Rollback();
			}

			if (this.isRoot)
			{
				this.transaction?.Dispose();
			}
		}
	}
}