// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	public class PostgresSqlTransactionalCommandsContext
		: DefaultSqlTransactionalCommandsContext
	{
		public PostgresSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, Transaction transaction)
			: base(sqlDatabaseContext, transaction)
		{
		}

		protected override DbType GetDbType(Type type)
		{
			var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

			if (underlyingType.IsEnum)
			{
				return DbType.Object;
			}

			return base.GetDbType(type);
		}
	}
}
