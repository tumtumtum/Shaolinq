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
			if (type.GetUnwrappedNullableType().IsEnum)
			{
				return new NpgsqlParameter(parameterName, NpgsqlDbType.Unknown) { Value = value };
			}
			else if (type.GetUnwrappedNullableType() == typeof(TimeSpan))
			{
				return new NpgsqlParameter(parameterName, NpgsqlDbType.Interval) { Value = value };
			}
			else
			{
				return base.CreateParameter(command, parameterName, type, value);
			}
		}

		protected override DbType GetDbType(Type type)
		{
			var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

			if (underlyingType.IsEnum)
			{
				return DbType.AnsiString;
			}

			return base.GetDbType(type);
		}
	}
}
