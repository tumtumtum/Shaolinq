// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Transactions;
using Devart.Data.PostgreSql;

namespace Shaolinq.Persistence.Sql.DevartPostgres
{
	public class DevartPostgresSqlPersistenceTransactionContext
		: SqlPersistenceTransactionContext
	{
		private static volatile Dictionary<CommandKey, CommandValue> CachedCommandsForInsert = new Dictionary<CommandKey, CommandValue>(CommandKeyComparer.Default);
		private static volatile Dictionary<CommandKey, CommandValue> CachedCommandsForUpdate = new Dictionary<CommandKey, CommandValue>(CommandKeyComparer.Default);

		protected override char ParameterIndicatorChar
		{
			get
			{
				return '@';
			}
		}

		private readonly Transaction transaction;
        private PgSqlTransaction dbTransaction;

		public DevartPostgresSqlPersistenceTransactionContext(SqlPersistenceContext persistenceContext, BaseDataAccessModel dataAccessModel, Transaction transaction)
			: base(persistenceContext, dataAccessModel, transaction)
		{
			this.transaction = transaction;

			if (this.transaction != null)
			{
                dbTransaction = (PgSqlTransaction)this.DbConnection.BeginTransaction(GetIsolationLevel(this.transaction.IsolationLevel));
			}
		}

		private System.Data.IsolationLevel GetIsolationLevel(System.Transactions.IsolationLevel isolationLevel)
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

		protected override IDbCommand CreateCommand()
		{
			var retval = (((PgSqlConnection)this.DbConnection).CreateCommand());
            
			retval.Transaction = this.dbTransaction;
			retval.CommandTimeout = (int)this.PersistenceContext.CommandTimeout.TotalSeconds;
			
			return retval;
		}
        
		protected override bool IsDataAccessException(Exception e)
		{
			return e is PgSqlException;
		}

		protected override bool IsConcurrencyException(Exception e)
		{
			var postgresException = e as PgSqlException;

			return postgresException != null && postgresException.ErrorCode == "40001";
		}

		public override void Dispose()
		{
			if (this.dbTransaction != null)
			{
				return; 
			}

			RealDispose();
		}
        
		private void RealDispose()
		{
			if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
			{
				if (this.DbConnection != null)
				{
					this.DbConnection.Close();
				}

				GC.SuppressFinalize(this);
			}
		}

		protected override object GetLastInsertedAutoIncrementValue(string tableName, string columnName, bool isSingularPrimaryKeyValue)
		{
			if (!isSingularPrimaryKeyValue)
			{
				throw new NotSupportedException();
			}

			var command = this.DbConnection.CreateCommand();

			command.CommandText = String.Concat("SELECT currval('\"", tableName, "_", columnName, "_seq\"')");

			try
			{
				return command.ExecuteScalar();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);

				throw;
			}
		}

		protected override Dictionary<CommandKey, CommandValue> InsertCache
		{
			get
			{
				return CachedCommandsForInsert;
			}
			set
			{
				CachedCommandsForInsert = value;
			}
		}

		protected override Dictionary<CommandKey, CommandValue> UpdateCache
		{
			get
			{
				return CachedCommandsForUpdate;
			}
			set
			{
				CachedCommandsForUpdate = value;
			}
		}

		protected override string InsertDefaultString
		{
			get
			{
				return null;
			}
		}

		public override void Commit()
		{
			if (this.dbTransaction != null)
			{
				this.dbTransaction.Commit();
				this.dbTransaction = null;
			}

			RealDispose();
		}

		public override void Rollback()
		{
			if (this.dbTransaction != null)
			{
				this.dbTransaction.Rollback();
				this.dbTransaction = null;
			}

			RealDispose();
		}
	}
}
