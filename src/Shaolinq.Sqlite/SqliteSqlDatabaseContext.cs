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
		private const int SQLITE_CONSTRAINT = 19;

		public string FileName { get; private set; }
		public override string GetConnectionString()
		{
			return connectionString;
		}

		private readonly string connectionString;

		public static SqliteSqlDatabaseContext Create(SqliteSqlDatabaseContextInfo contextInfo, ConstraintDefaults constraintDefaults)
		{
			var sqlDataTypeProvider = new SqliteSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(SqliteSqlDialect.Default, sqlDataTypeProvider, typeof(SqliteSqlQueryFormatter));

			return new SqliteSqlDatabaseContext(contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager);
		}

		private SqliteSqlDatabaseContext(SqliteSqlDatabaseContextInfo contextInfo, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager)
			: base(SqliteSqlDialect.Default, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo)
		{
			this.FileName = contextInfo.FileName;

			connectionString = "Data Source=" + this.FileName + ";foreign keys=True";
		}

		internal SqliteSqlDatabaseTransactionContext inMemoryContext;

		public override DatabaseTransactionContext CreateDatabaseTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
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

		public override DatabaseCreator CreateDatabaseCreator(DataAccessModel model)
		{
			return new SqliteDatabaseCreator(this, model);
		}
        
		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(databaseTransactionContext);	
		}

		public override Exception DecorateException(Exception exception, string relatedQuery)
		{
			// http://www.sqlite.org/c3ref/c_abort.html

			var sqliteException = exception as SQLiteException;

			if (sqliteException == null)
			{
				return base.DecorateException(exception, relatedQuery);
			}
			
			if (sqliteException.ErrorCode == SQLITE_CONSTRAINT)
			{
				return new UniqueKeyConstraintException(exception, relatedQuery);
			}
			
			return new DataAccessException(exception, relatedQuery);
		}
	}
}
