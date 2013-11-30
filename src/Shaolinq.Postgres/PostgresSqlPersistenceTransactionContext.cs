// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Transactions;
using Npgsql;
﻿using Shaolinq.Persistence.Sql;
﻿using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres
{
	/// <summary>
	/// A Postgres specific <see cref="SqlPersistenceTransactionContext"/>.
	/// </summary>
	public class PostgresSqlPersistenceTransactionContext
		: PostgresSharedSqlPersistenceTransactionContext
	{
		private static volatile Dictionary<CommandKey, CommandValue> CachedCommandsForInsert = new Dictionary<CommandKey, CommandValue>(CommandKeyComparer.Default);
		private static volatile Dictionary<CommandKey, CommandValue> CachedCommandsForUpdate = new Dictionary<CommandKey, CommandValue>(CommandKeyComparer.Default);

		private readonly Transaction transaction;
		private NpgsqlTransaction dbTransaction;

		public PostgresSqlPersistenceTransactionContext(SqlPersistenceContext persistenceContext, BaseDataAccessModel dataAccessModel, Transaction transaction)
			: base(persistenceContext, dataAccessModel, transaction)
		{
			this.transaction = transaction;

			if (this.transaction != null)
			{
				dbTransaction = (NpgsqlTransaction)this.DbConnection.BeginTransaction(GetIsolationLevel(this.transaction.IsolationLevel));
			}
		}

		protected override string GetRelatedSql(Exception e)
		{
			var postgresException = e as NpgsqlException;

			if (postgresException == null)
			{
				return "";
			}

			return postgresException.ErrorSql;
		}

		protected override bool IsDataAccessException(Exception e)
		{
			return e is NpgsqlException;
		}

		protected override bool IsConcurrencyException(Exception e)
		{
			var postgresException = e as NpgsqlException;

			return postgresException != null && postgresException.Code == "40001";
		}

		private static System.Data.IsolationLevel GetIsolationLevel(System.Transactions.IsolationLevel isolationLevel)
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
			var retval = base.CreateCommand();

			retval.Transaction = this.dbTransaction;
			
			return retval;
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
