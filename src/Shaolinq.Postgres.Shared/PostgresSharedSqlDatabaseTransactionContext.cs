using System;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public abstract class PostgresSharedSqlDatabaseTransactionContext
		: DefaultSqlDatabaseTransactionContext
	{
		protected PostgresSharedSqlDatabaseTransactionContext(SqlDatabaseContext sqlDatabaseContext, DataAccessModel dataAccessModel, Transaction transaction)
			: base(sqlDatabaseContext, dataAccessModel, transaction)
		{
		}
	}
}
