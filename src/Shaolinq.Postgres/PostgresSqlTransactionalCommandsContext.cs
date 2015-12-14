// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Transactions;
using Shaolinq.Persistence;
using Npgsql;
using NpgsqlTypes;
using Platform;

namespace Shaolinq.Postgres
{
	public class PostgresSqlTransactionalCommandsContext
		: DefaultSqlTransactionalCommandsContext
	{
		public PostgresSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, Transaction transaction)
			: base(sqlDatabaseContext, transaction)
		{
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
