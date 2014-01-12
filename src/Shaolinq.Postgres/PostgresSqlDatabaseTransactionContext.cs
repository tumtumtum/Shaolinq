// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Threading;
using System.Transactions;
using Npgsql;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres
{
	/// <summary>
	/// A Postgres specific <see cref="DefaultSqlDatabaseTransactionContext"/>.
	/// </summary>
	public class PostgresSqlDatabaseTransactionContext
		: PostgresSharedSqlDatabaseTransactionContext
	{
		private NpgsqlTransaction dbTransaction; 
		private readonly Transaction transaction;
		
		public PostgresSqlDatabaseTransactionContext(SqlDatabaseContext sqlDatabaseContext, Transaction transaction)
			: base(sqlDatabaseContext, transaction)
		{
			this.transaction = transaction;

			if (this.transaction != null)
			{
				dbTransaction = (NpgsqlTransaction)this.DbConnection.BeginTransaction(GetIsolationLevel(this.transaction.IsolationLevel));
			}
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

		public override IDbCommand CreateCommand(SqlCreateCommandOptions options)
		{
			var retval = base.CreateCommand(options);

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
