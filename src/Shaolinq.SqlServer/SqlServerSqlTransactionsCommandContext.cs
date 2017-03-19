// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlTransactionsCommandContext
		: DefaultSqlTransactionalCommandsContext
	{
		public SqlServerSqlTransactionsCommandContext(SqlDatabaseContext sqlDatabaseContext, IDbConnection connection, TransactionContext transactionContext)
			: base(sqlDatabaseContext, connection, transactionContext)
		{
		}

		protected override DbType GetDbType(Type type)
		{
			var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

			if (underlyingType == typeof(DateTime))
			{
				return DbType.DateTime2;
			}

			return base.GetDbType(type);
		}
	}
}
