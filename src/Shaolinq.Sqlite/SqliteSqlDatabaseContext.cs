// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
using System.Transactions;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public abstract class SqliteSqlDatabaseContext
		: SqlDatabaseContext
	{
		public string FileName { get; private set; }

		
		protected SqliteSqlDatabaseContext(DataAccessModel model, SqliteSqlDatabaseContextInfo contextInfo, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager)
			: base(model, SqliteSqlDialect.Default, sqlDataTypeProvider, sqlQueryFormatterManager, Path.GetFileNameWithoutExtension(contextInfo.FileName), contextInfo)
		{
			this.FileName = contextInfo.FileName;

			this.ConnectionString = "Data Source=" + this.FileName + ";foreign keys=True";

			if (SqliteSqlDatabaseContext.IsRunningMono())
			{
				this.SchemaManager = new SqliteMonoSqlDatabaseSchemaManager(this);
			}
			else
			{
				this.SchemaManager = new SqliteWindowsSqlDatabaseSchemaManager(this);
			}
		}

		internal static bool IsRunningMono()
		{
			return Type.GetType("Mono.Runtime") != null;
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

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlDatabaseTransactionContext sqlDatabaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(sqlDatabaseTransactionContext);	
		}
	}
}
