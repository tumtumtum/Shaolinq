// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Data.SQLite;
using System.Transactions;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDatabaseContext
		: SystemDataBasedSqlDatabaseContext
	{
		public string FileName { get; private set; }
		public override string GetConnectionString()
		{
			return connectionString;
		}

		private readonly string connectionString;
		
		public SqliteSqlDatabaseContext(SqliteSqlDatabaseContextInfo contextInfo)
			: base(SqliteSqlDialect.Default, SqliteSqlDataTypeProvider.Instance, new DefaultSqlQueryFormatterManager(SqliteSqlDialect.Default, SqliteSqlDataTypeProvider.Instance, typeof(SqliteSqlQueryFormatter)), contextInfo)
		{
			this.FileName = contextInfo.FileName;

			connectionString = "Data Source=" + this.FileName + ";foreign keys=True";
		}

		internal SqliteSqlDatabaseTransactionContext inMemoryContext;

		public override DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			if (String.Equals(this.FileName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				if (inMemoryContext == null)
				{
					inMemoryContext = new SqliteSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
				}

				return inMemoryContext;
			}

			return new SqliteSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return new SQLiteFactory();
		}

		public override DatabaseCreator NewDatabaseCreator(DataAccessModel model)
		{
			return new SqliteDatabaseCreator(this, model);
		}
        
		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(databaseTransactionContext);	
		}
	}
}
