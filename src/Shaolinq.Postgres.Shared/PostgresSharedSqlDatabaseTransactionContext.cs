using System;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public abstract class PostgresSharedSqlDatabaseTransactionContext
		: SqlDatabaseTransactionContext
	{
		private readonly string schemaName;

		protected override char ParameterIndicatorChar
		{
			get
			{
				return '@';
			}
		}

		protected PostgresSharedSqlDatabaseTransactionContext(SystemDataBasedSqlDatabaseContext sqlDatabaseContext, DataAccessModel dataAccessModel, Transaction transaction)
			: base(sqlDatabaseContext, dataAccessModel, transaction)
		{
			this.schemaName = this.SqlDatabaseContext.SchemaName;
		}
	}
}
