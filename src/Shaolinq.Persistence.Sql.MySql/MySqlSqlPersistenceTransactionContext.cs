using System;
using System.Collections.Generic;
using System.Transactions;
using MySql.Data.MySqlClient;

namespace Shaolinq.Persistence.Sql.MySql
{
	public class MySqlSqlPersistenceTransactionContext
		: SqlPersistenceTransactionContext
	{
		private static volatile Dictionary<CommandKey, CommandValue> CachedCommandsForInsert = new Dictionary<CommandKey, CommandValue>(CommandKeyComparer.Default);
		private static volatile Dictionary<CommandKey, CommandValue> CachedCommandsForUpdate = new Dictionary<CommandKey, CommandValue>(CommandKeyComparer.Default);

		protected override char ParameterIndicatorChar
		{
			get
			{
				return '?';
			}
		}

		protected override bool IsDataAccessException(Exception e)
		{
			return e is MySqlException;
		}

		protected override bool IsConcurrencyException(Exception e)
		{
			return false;
		}

		public MySqlSqlPersistenceTransactionContext(SqlPersistenceContext persistenceContext, BaseDataAccessModel dataAccessModel, Transaction transaction)
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
			
			command.CommandText = "select last_insert_id()";

			return command.ExecuteScalar();
		}

		protected override Dictionary<CommandKey, CommandValue> InsertCache
		{
			get
			{
				return CachedCommandsForInsert;
			}
			set
			{
				CachedCommandsForInsert = value;
			}
		}

		protected override Dictionary<CommandKey, CommandValue> UpdateCache
		{
			get
			{
				return CachedCommandsForUpdate;
			}
			set
			{
				CachedCommandsForUpdate = value;
			}
		}

		protected override string InsertDefaultString
		{
			get
			{
				return null;
			}
		}
	}
}
