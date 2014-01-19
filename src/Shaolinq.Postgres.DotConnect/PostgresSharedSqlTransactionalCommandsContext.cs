using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresSharedSqlTransactionalCommandsContext
		: DefaultSqlTransactionalCommandsContext
	{
		public PostgresSharedSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, Transaction transaction)
			: base(sqlDatabaseContext, transaction)
		{
		}
	}
}
