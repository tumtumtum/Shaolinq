// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	public partial class PostgresSqlTransactionalCommandsContext
		: DefaultSqlTransactionalCommandsContext
	{
		private string preparedTransactionName;

		public PostgresSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, IDbConnection connection, TransactionContext transactionContext)
			: base(sqlDatabaseContext, connection, transactionContext)
		{	
		}

		public override void Prepare()
		{
			if (this.preparedTransactionName != null)
			{
				throw new InvalidOperationException("Transaction already prepared");
			}

			this.preparedTransactionName = Guid.NewGuid().ToString("N");

			using (var command = this.CreateCommand())
			{
				command.CommandText = $"PREPARE TRANSACTION '{this.preparedTransactionName}';";
				command.ExecuteNonQuery();
			}

			this.dbTransaction = null;
		}

		[RewriteAsync]
		public override void Commit()
		{
			if (this.preparedTransactionName != null)
			{
				using (var command = this.CreateCommand())
				{
					command.CommandText = $"COMMIT PREPARED '{this.preparedTransactionName}';";
					command.ExecuteNonQueryEx(this.DataAccessModel);
				}
			}

			base.Commit();
		}
		
		[RewriteAsync]
		public override void Rollback()
		{
			if (this.preparedTransactionName != null)
			{
				using (var command = this.CreateCommand())
				{
					command.CommandText = $"ROLLBACK PREPARED '{this.preparedTransactionName}';";
					command.ExecuteNonQueryEx(this.DataAccessModel);
				}
			}

			base.Rollback();
		}
		
		protected override IDbDataParameter CreateParameter(IDbCommand command, string parameterName, Type type, object value)
		{
			var unwrapped = type.GetUnwrappedNullableType();

			if (unwrapped.IsEnum)
			{
				return new NpgsqlParameter(parameterName, NpgsqlDbType.Unknown) { Value = value ?? DBNull.Value };
			}

			if (unwrapped == typeof(TimeSpan))
			{
				return new NpgsqlParameter(parameterName, NpgsqlDbType.Interval) { Value = value ?? DBNull.Value };
			}

			return base.CreateParameter(command, parameterName, type, value);
		}

		protected override DbType GetDbType(Type type)
		{
			var unwrapped = type.GetUnwrappedNullableType();

			if (unwrapped.IsEnum)
			{
				return DbType.AnsiString;
			}

			return base.GetDbType(type);
		}
	}
}
