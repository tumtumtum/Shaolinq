// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence
{
	public abstract class SqlTransactionalCommandsContext
		: IDisposable
	{
		internal MarsDataReader currentReader;

		private bool disposed;
		public bool SupportsAsync { get; }
		public Transaction Transaction { get; }
		public IDbConnection DbConnection { get; private set; }
		public SqlDatabaseContext SqlDatabaseContext { get; }
		
		protected IDbTransaction dbTransaction;
		public DataAccessModel DataAccessModel { get; }
		private readonly bool emulateMultipleActiveResultSets;

		public abstract void Delete(SqlDeleteExpression deleteExpression);
		public abstract void Delete(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract void Update(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract InsertResults Insert(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract IDataReader ExecuteReader(string sql, IReadOnlyList<Tuple<Type, object>> parameters);

		public virtual bool IsClosed => this.DbConnection.State == ConnectionState.Closed || this.DbConnection.State == ConnectionState.Broken;

		public virtual Task DeleteAsync(SqlDeleteExpression deleteExpression, CancellationToken cancellationToken)
		{
			this.Delete(deleteExpression);

			return Task.FromResult<object>(null);
		}

		public virtual Task DeleteAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken)
		{
			this.Delete(type, dataAccessObjects);

			return Task.FromResult<object>(null);
		}

		public virtual Task UpdateAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken)
		{
			this.Update(type, dataAccessObjects);

			return Task.FromResult<object>(null);
		}

		public virtual Task<InsertResults> InsertAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken)
		{
			return Task.FromResult(this.Insert(type, dataAccessObjects));
		}

		public virtual Task<IDataReader> ExecuteReaderAsync(string sql, IReadOnlyList<Tuple<Type, object>> parameters, CancellationToken cancellationToken)
		{
			return Task.FromResult(this.ExecuteReader(sql, parameters));
		}

        protected SqlTransactionalCommandsContext()
			: this(true)
		{
		}

        protected SqlTransactionalCommandsContext(bool supportsAsync)
		{
			this.SupportsAsync = supportsAsync;
		}

		public static System.Data.IsolationLevel ConvertIsolationLevel(System.Transactions.IsolationLevel isolationLevel)
		{
			switch (isolationLevel)
			{
			case System.Transactions.IsolationLevel.Serializable:
				return System.Data.IsolationLevel.Serializable;
			case System.Transactions.IsolationLevel.ReadCommitted:
				return System.Data.IsolationLevel.ReadCommitted;
			case System.Transactions.IsolationLevel.Chaos:
				return System.Data.IsolationLevel.Chaos;
			case System.Transactions.IsolationLevel.RepeatableRead:
				return System.Data.IsolationLevel.RepeatableRead;
			case System.Transactions.IsolationLevel.Snapshot:
				return System.Data.IsolationLevel.Snapshot;
			default:
				return System.Data.IsolationLevel.Unspecified;
			}
		}

		protected SqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, IDbConnection dbConnection, Transaction transaction)
		{	
			this.DbConnection = dbConnection;
			this.Transaction = transaction;
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.DataAccessModel = sqlDatabaseContext.DataAccessModel;

			this.emulateMultipleActiveResultSets = !sqlDatabaseContext.SqlDialect.SupportsCapability(SqlCapability.MultipleActiveResultSets);

			if (transaction != null)
			{
				this.dbTransaction = dbConnection.BeginTransaction(ConvertIsolationLevel(transaction.IsolationLevel));
			}
		}

		~SqlTransactionalCommandsContext()
		{
			this.Dispose(false);
		}

		public virtual IDbCommand CreateCommand() => this.CreateCommand(SqlCreateCommandOptions.Default);

		public virtual IDbCommand CreateCommand(SqlCreateCommandOptions options)
		{
			var retval = this.DbConnection.CreateCommand();

			retval.Transaction = this.dbTransaction;

			if (this.SqlDatabaseContext.CommandTimeout != null)
			{
				retval.CommandTimeout = (int)this.SqlDatabaseContext.CommandTimeout.Value.TotalMilliseconds;
			}

			if (this.emulateMultipleActiveResultSets)
			{
				retval = new MarsDbCommand(this, retval);
			}

			return retval;
		}

		public virtual void Commit()
		{
			try
			{
				if (this.dbTransaction != null)
				{
					this.dbTransaction.Commit();

					this.dbTransaction = null;
				}
			}
			catch (Exception e)
			{
				var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e);
				var decoratedException = this.SqlDatabaseContext.DecorateException(e, null, relatedSql);

				if (decoratedException != e)
				{
					throw decoratedException;
				}

				throw;
			}

			this.CloseConnection();
		}

		public virtual void Rollback()
		{
			if (this.dbTransaction != null)
			{
				this.dbTransaction.Rollback();

				this.dbTransaction = null;
			}

			this.CloseConnection();
		}

		protected virtual void CloseConnection()
		{
			if (this.DbConnection != null)
			{
				this.DbConnection.Close();

				this.DbConnection = null;
			}

			GC.SuppressFinalize(this);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.dbTransaction != null)
			{
				return;
			}

			if (this.disposed)
			{
				return;
			}

			ActionUtils.IgnoreExceptions(() => currentReader?.Dispose());
			ActionUtils.IgnoreExceptions(this.CloseConnection);

			this.disposed = true;
		}
	}
}
