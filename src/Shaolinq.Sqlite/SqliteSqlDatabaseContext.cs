// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Transactions;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDatabaseContext
		: SqlDatabaseContext
	{
		public string FileName { get; private set; }

		public static SqliteSqlDatabaseContext Create(SqliteSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaults;
			var sqlDataTypeProvider = new SqliteSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(SqliteSqlDialect.Default, sqlDataTypeProvider, typeof(SqliteSqlQueryFormatter));

			return new SqliteSqlDatabaseContext(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager);
		}

		private SqliteSqlDatabaseContext(DataAccessModel model, SqliteSqlDatabaseContextInfo contextInfo, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager)
			: base(model, SqliteSqlDialect.Default, sqlDataTypeProvider, sqlQueryFormatterManager, Path.GetFileNameWithoutExtension(contextInfo.FileName), contextInfo)
		{
			this.FileName = contextInfo.FileName;

			this.ConnectionString = "Data Source=" + this.FileName + ";foreign keys=True";
			this.SchemaManager = new SqliteSqlDatabaseSchemaManager(this);
		}

		internal SqliteSqlDatabaseTransactionContext inMemoryContext;

		public override SqlDatabaseTransactionContext CreateDatabaseTransactionContext(Transaction transaction)
		{
			if (String.Equals(this.FileName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				if (inMemoryContext == null)
				{
					inMemoryContext = new SqliteSqlDatabaseTransactionContext(this, transaction);
				}

				return inMemoryContext;
			}

			return new SqliteSqlDatabaseTransactionContext(this, transaction);
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return new SQLiteFactory();
		}

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlDatabaseTransactionContext sqlDatabaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(sqlDatabaseTransactionContext);	
		}

		public override Exception DecorateException(Exception exception, string relatedQuery)
		{
			// http://www.sqlite.org/c3ref/c_abort.html

			var sqliteException = exception as SQLiteException;

			if (sqliteException == null)
			{
				return base.DecorateException(exception, relatedQuery);
			}
			
			if (sqliteException.ErrorCode == SqliteErrorCodes.SqliteConstraint)
			{
				return new UniqueKeyConstraintException(exception, relatedQuery);
			}
			
			return new DataAccessException(exception, relatedQuery);
		}
	}
}
