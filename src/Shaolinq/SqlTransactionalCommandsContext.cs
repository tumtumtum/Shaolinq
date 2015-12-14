// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq
{
	public abstract class SqlTransactionalCommandsContext
		: IDisposable
	{
		private int disposed;
		public bool SupportsAsync { get; }
		public Transaction Transaction { get; }
		public IDbConnection DbConnection { get; private set; }
		public SqlDatabaseContext SqlDatabaseContext { get; }
		
		protected IDbTransaction dbTransaction;
		public DataAccessModel DataAccessModel { get; }

		public virtual void Delete(SqlDeleteExpression deleteExpression)
		{
			this.DeleteAsync(deleteExpression).StartAndOrGetResult();
		}

		public virtual void Delete(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			this.DeleteAsync(type, dataAccessObjects).StartAndOrGetResult();
		}

		public virtual void Update(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			this.UpdateAsync(type, dataAccessObjects).StartAndOrGetResult();
		}

		public virtual InsertResults Insert(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			return this.InsertAsync(type, dataAccessObjects).StartAndOrGetResult();
		}
		
		public virtual IDataReader ExecuteReader(string sql, IReadOnlyList<Tuple<Type, object>> parameters)
		{
			return ExecuteReaderAsync(sql, parameters).StartAndOrGetResult();
		}

        public abstract Task<IDataReader> ExecuteReaderAsync(string sql, IReadOnlyList<Tuple<Type, object>> parameters);

        protected SqlTransactionalCommandsContext()
			: this(true)
		{
		}

        protected SqlTransactionalCommandsContext(bool supportsAsync)
		{
			this.SupportsAsync = supportsAsync;
		}

		public abstract Task DeleteAsync(SqlDeleteExpression deleteExpression);
		public abstract Task DeleteAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract Task UpdateAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract Task<InsertResults> InsertAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects);

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

			if (transaction != null)
			{
				this.dbTransaction = dbConnection.BeginTransaction(ConvertIsolationLevel(transaction.IsolationLevel));
			}
		}

		public virtual bool IsClosed => this.DbConnection.State == ConnectionState.Closed || this.DbConnection.State == ConnectionState.Broken;

		~SqlTransactionalCommandsContext()
		{
			this.Dispose();
		}

		public virtual IDbCommand CreateCommand()
		{
			var retval = this.CreateCommand(SqlCreateCommandOptions.Default);

			if (this.dbTransaction != null)
			{
				retval.Transaction = this.dbTransaction;
			}

			return retval;
		}

		public virtual IDbCommand CreateCommand(SqlCreateCommandOptions options)
		{
			var retval = this.DbConnection.CreateCommand();

			retval.Transaction = this.dbTransaction;

			if (this.SqlDatabaseContext.CommandTimeout != null)
			{
				retval.CommandTimeout = (int)this.SqlDatabaseContext.CommandTimeout.Value.TotalMilliseconds;
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

		public virtual void Dispose()
		{
			if (this.dbTransaction != null)
			{
				return;
			}

			if (Interlocked.CompareExchange(ref this.disposed, 1, 0) != 0)
			{
				return;
			}

			this.CloseConnection();
		}
	}
}
