using System;
using System.Transactions;
using Shaolinq.Persistence.Sql;

namespace Shaolinq.Postgres.Shared
{
	public abstract class PostgresSharedSqlPersistenceTransactionContext
		: SqlPersistenceTransactionContext
	{
		protected override char ParameterIndicatorChar
		{
			get
			{
				return '@';
			}
		}

		protected PostgresSharedSqlPersistenceTransactionContext(SqlPersistenceContext persistenceContext, BaseDataAccessModel dataAccessModel, Transaction transaction)
			: base(persistenceContext, dataAccessModel, transaction)
		{
		}

		protected override object GetLastInsertedAutoIncrementValue(string tableName, string columnName, bool isSingularPrimaryKeyValue)
		{
			if (!isSingularPrimaryKeyValue)
			{
				throw new NotSupportedException();
			}

			var command = this.DbConnection.CreateCommand();

			command.CommandText = String.Format("SELECT currval(pg_get_serial_sequence('\"{0}\"', '{1}'))", tableName, columnName);

			try
			{
				return command.ExecuteScalar();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);

				throw;
			}
		}
	}
}
