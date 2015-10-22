// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlTransactionsCommandContext
		: DefaultSqlTransactionalCommandsContext
	{
		public SqlServerSqlTransactionsCommandContext(SqlDatabaseContext sqlDatabaseContext, Transaction transaction)
			: base(sqlDatabaseContext, transaction)
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
