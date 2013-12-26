// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Transactions;
﻿using Shaolinq.Persistence.Sql;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDatabaseTransactionContext
		: SqlDatabaseTransactionContext
	{
		private static volatile Dictionary<CommandKey, CommandValue> CachedCommandsForInsert = new Dictionary<CommandKey, CommandValue>(CommandKeyComparer.Default);
		private static volatile Dictionary<CommandKey, CommandValue> CachedCommandsForUpdate = new Dictionary<CommandKey, CommandValue>(CommandKeyComparer.Default);
        
		protected override char ParameterIndicatorChar
		{
			get
			{
				return '@';
			}
		}

		protected override bool IsDataAccessException(Exception e)
		{
			return e is SQLiteException;
		}
        
		protected override bool IsConcurrencyException(Exception e)
		{
			return false;
		}

		public SqliteSqlDatabaseTransactionContext(SystemDataBasedDatabaseConnection databaseConnection, DataAccessModel dataAccessModel, Transaction transaction)
			: base(databaseConnection, dataAccessModel, transaction)
		{
		}

		public virtual void RealDispose()
		{
			base.Dispose();
		}

		public override void Dispose()
		{
			if (!String.Equals(this.DatabaseConnection.DatabaseName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				base.Dispose();
			}
		}

		protected override object GetLastInsertedAutoIncrementValue(string tableName, string columnName, bool isSingularPrimaryKeyValue)
		{
			if (!isSingularPrimaryKeyValue)
			{
				throw new NotSupportedException();
			}

			var command = this.DbConnection.CreateCommand();
			
			command.CommandText = "select last_insert_rowid()";

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
				return "DEFAULT VALUES";
			}
		}
	}
}
