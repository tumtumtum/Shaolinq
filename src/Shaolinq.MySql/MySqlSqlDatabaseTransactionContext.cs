// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Transactions;
using MySql.Data.MySqlClient;
﻿using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public class MySqlSqlDatabaseTransactionContext
		: SqlDatabaseTransactionContext
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

		public MySqlSqlDatabaseTransactionContext(SystemDataBasedSqlDatabaseContext sqlDatabaseContext, DataAccessModel dataAccessModel, Transaction transaction)
			: base(sqlDatabaseContext, dataAccessModel, transaction)
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
