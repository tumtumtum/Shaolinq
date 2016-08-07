// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public partial class DataAccessScope
		: IDisposable
	{
		public DataAccessIsolationLevel IsolationLevel { get; }

		private bool complete;
		private bool disposed;
		private readonly bool isRoot;
		private readonly DataAccessTransaction outerTransaction;
		private readonly TransactionScope nativeScope;
		private readonly DataAccessScopeOptions options;
		private readonly DataAccessTransaction transaction;
		
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

		public DataAccessScope(DataAccessScopeOptions options)
			: this(DataAccessIsolationLevel.Unspecified, options, TimeSpan.Zero)
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
					this.transaction = new DataAccessTransaction(isolationLevel, timeout);
					DataAccessTransaction.Current = this.transaction;
				}
				else
				{
					this.transaction = currentTransaction;
					this.outerTransaction = currentTransaction;
				}
				break;
			case DataAccessScopeOptions.RequiresNew:
				this.isRoot = true;
				this.outerTransaction = currentTransaction;
				if (Transaction.Current != null)
				{
					this.nativeScope = new TransactionScope(TransactionScopeOption.RequiresNew);
				}
				this.transaction = new DataAccessTransaction(isolationLevel, timeout);
				DataAccessTransaction.Current = this.transaction;
				break;
			case DataAccessScopeOptions.Suppress:
				if (Transaction.Current != null)
				{
					this.nativeScope = new TransactionScope(TransactionScopeOption.Suppress);
				}
				if (currentTransaction != null)
				{
					this.outerTransaction = currentTransaction;
					DataAccessTransaction.Current = null;
				}
				break;
			}
		}

		/// <summary>
		/// Flushes the current transaction for all <see cref="DataAccessModel"/> that have
		/// participated in the current transaction
		/// </summary>
		/// <remarks>
		/// Flushing a transaction writes any pending INSERTs, UPDATES and DELETES to the database
		/// but does not commit the transaction. To commit the transaction you must call 
		/// <see cref="Complete(ScopeCompleteOptions)"/>.
		/// </remarks>
		[RewriteAsync]
		public void Flush()
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

		/// <summary>
		/// Flushes the current transaction for the given <paramref name="dataAccessModel"/>
		/// </summary>
		/// <remarks>
		/// Flushing a transaction writes any pending INSERTs, UPDATES and DELETES to the database
		/// but does not commit the transaction. To commit the transaction you must call 
		/// <see cref="Complete(ScopeCompleteOptions)"/>.
		/// </remarks>
		/// <param name="dataAccessModel">
		/// The <see cref="DataAccessModel"/> to flush if you only want to flush a single
		/// DataAccessModel
		/// </param>
		[RewriteAsync]
		public void Flush(DataAccessModel dataAccessModel)
		{
			this.transaction.CheckAborted();

			if (!dataAccessModel.IsDisposed)
			{
				dataAccessModel.Flush();
			}
		}

		/// <summary>
		/// Flushes the current transaction and marks the scope as completed
		/// </summary>
		/// <remarks>
		/// <para>
		/// Flushing a scope commits 
		/// </para>
		/// <para>
		/// By default all nested scopes auto-flush without commiting the transaction. You can
		/// disable auto-flush by calling <see cref="Complete(ScopeCompleteOptions)"/>
		/// </para>
		/// </remarks>
		[RewriteAsync]
		public void Complete()
		{
			this.Complete(ScopeCompleteOptions.Default);
		}

		/// <summary>
		/// Flushes the current transaction and marks the scope as completed
		/// </summary>
		/// <remarks>
		/// <para>
		/// Flushing a scope commits 
		/// </para>
		/// <para>
		/// A scope is considered to have aborted if Complete is not called before the scope is disposed
		/// The outer most scope flushes and commits the transaction when it is completed.
		/// </para>
		/// <para>
		/// By default all nested scopes auto-flush without commiting the transaction. You can
		/// disable auto-flush by calling <see cref="Complete(ScopeCompleteOptions)"/>
		/// </para>
		/// </remarks>
		/// <param name="options">Set to <a cref="ScopeCompleteOptions.SuppressAutoFlush"/> to suppress auto-flush</param>
		[RewriteAsync]
		public void Complete(ScopeCompleteOptions options)
		{
			this.complete = true;

			this.transaction?.CheckAborted();

			if ((options & ScopeCompleteOptions.SuppressAutoFlush) != 0)
			{
				this.Flush();
			}

			if (this.transaction == null)
			{
				DataAccessTransaction.Current = this.outerTransaction;

				return;
			}

			if (!this.isRoot)
			{
				DataAccessTransaction.Current = this.outerTransaction;

				return;
			}

			if (this.transaction.HasSystemTransaction)
			{
				return;
			}

			if (this.transaction != DataAccessTransaction.Current)
			{
				throw new InvalidOperationException($"Cannot commit {this.GetType().Name} within another Async/Call context");
			}
			
			this.transaction.Commit();
			this.transaction.Dispose();
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

			DataAccessTransaction.Current = this.outerTransaction;

			this.nativeScope?.Dispose();
		}
	}
}