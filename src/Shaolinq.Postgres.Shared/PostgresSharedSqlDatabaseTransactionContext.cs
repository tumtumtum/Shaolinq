using System;
using System.Transactions;
using Shaolinq.Persistence;
using log4net;

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

		protected override object GetLastInsertedAutoIncrementValue(string tableName, string columnName, bool isSingularPrimaryKeyValue)
		{
			if (!isSingularPrimaryKeyValue)
			{
				throw new NotSupportedException();
			}

			var command = this.DbConnection.CreateCommand();

			if (string.IsNullOrEmpty(this.schemaName))
			{
				command.CommandText = String.Format("SELECT currval(pg_get_serial_sequence('\"{0}\"', '{1}'))", tableName, columnName);
			}
			else
			{
				command.CommandText = String.Format("SELECT currval(pg_get_serial_sequence('\"{2}\".\"{0}\"', '{1}'))", tableName, columnName, this.SqlDatabaseContext.SchemaName);
			}

			if (Logger.IsDebugEnabled)
			{
				Logger.Debug(command.CommandText);
			}

			try
			{
				return command.ExecuteScalar();
			}
			catch (Exception e)
			{
				Logger.Error(e.ToString());

				throw;
			}
		}
	}
}
