// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Shaolinq.Persistence;
using Npgsql;
using NpgsqlTypes;
using Platform;
using System.Threading.Tasks;

namespace Shaolinq.Postgres
{
	public class PostgresSqlTransactionalCommandsContext
		: DefaultSqlTransactionalCommandsContext
	{
		private string preparedTransactionName;
		private static readonly Func<NpgsqlTransaction, CancellationToken, Task> commitAsyncFunc;

		static PostgresSqlTransactionalCommandsContext()
		{
			var commitAsyncMethod = typeof(NpgsqlTransaction).GetMethod("CommitAsync", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(CancellationToken) }, null);

			if (commitAsyncMethod != null)
			{
				var param1 = Expression.Parameter(typeof(NpgsqlTransaction));
				var param2 = Expression.Parameter(typeof(CancellationToken));

				var body = Expression.Call(param1, commitAsyncMethod, param2);

				commitAsyncFunc = Expression.Lambda<Func<NpgsqlTransaction, CancellationToken, Task>>(body, param1, param2).Compile();
			}
		}

		public PostgresSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, DataAccessTransaction transaction)
			: base(sqlDatabaseContext, transaction)
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

		protected override Task CommitTransactionAsync(CancellationToken cancellationToken)
		{
			var transaction = (NpgsqlTransaction)this.dbTransaction;

			if (commitAsyncFunc == null)
			{
				this.dbTransaction.Commit();

				return Task.FromResult<object>(null);
			}
			else
			{
				return commitAsyncFunc(transaction, cancellationToken);
			}
		}

		public override void Commit()
		{
			if (this.preparedTransactionName != null)
			{
				using (var command = this.CreateCommand())
				{
					command.CommandText = $"COMMIT PREPARED '{this.preparedTransactionName}';";
					command.ExecuteNonQuery();
				}
			}

			base.Commit();
		}

		public override void Rollback()
		{
			if (this.preparedTransactionName != null)
			{
				using (var command = this.CreateCommand())
				{
					command.CommandText = $"ROLLBACK PREPARED '{this.preparedTransactionName}';";
					command.ExecuteNonQuery();
				}
			}

			base.Rollback();
		}
		protected override IDbDataParameter CreateParameter(IDbCommand command, string parameterName, Type type, object value)
		{
			var unwrapped = type.GetUnwrappedNullableType();

            if (unwrapped.IsEnum)
			{
				return new NpgsqlParameter(parameterName, NpgsqlDbType.Unknown) { Value = value };
			}

			if (unwrapped == typeof(TimeSpan))
			{
				return new NpgsqlParameter(parameterName, NpgsqlDbType.Interval) { Value = value };
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
