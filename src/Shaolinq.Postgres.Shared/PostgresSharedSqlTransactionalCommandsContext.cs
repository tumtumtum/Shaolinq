using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedSqlTransactionalCommandsContext
		: DefaultSqlTransactionalCommandsContext
	{
		public PostgresSharedSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, Transaction transaction)
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
