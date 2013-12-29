// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq.Expressions;
using System.Transactions;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Sql;
﻿using Shaolinq.Persistence.Sql.Linq;

namespace Shaolinq.Sqlite
{
	public class SqliteDatabaseConnection
		: SystemDataBasedDatabaseConnection
	{
		public override string GetConnectionString()
		{
			return connectionString;
		}

		private readonly string connectionString;
		
		public SqliteDatabaseConnection(string fileName, string schemaNamePrefix)
			: base(fileName, SqliteSqlDialect.Default, SqliteSqlDataTypeProvider.Instance)
		{
			this.SchemaNamePrefix = EnvironmentSubstitutor.Substitute(schemaNamePrefix);
			connectionString = "Data Source=" + this.DatabaseName + ";foreign keys=True";
		}

		internal SqliteSqlDatabaseTransactionContext inMemoryContext;

		public override DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			if (String.Equals(this.DatabaseName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				if (inMemoryContext == null)
				{
					inMemoryContext = new SqliteSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
				}

				return inMemoryContext;
			}

			return new SqliteSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
		}

		public override Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
		{
			return new SqliteSqlQueryFormatter(dataAccessModel, sqlDataTypeProvider, sqlDialect, expression, options);
		}

		public override DbProviderFactory NewDbProviderFactory()
		{
			return new SQLiteFactory();
		}

		public override TableDescriptor GetTableDescriptor(string tableName)
		{
			throw new NotImplementedException();
		}

		public override SqlSchemaWriter NewSqlSchemaWriter(DataAccessModel model)
		{
			return new SqlSchemaWriter(this, model);
		}

		public override DatabaseCreator NewDatabaseCreator(DataAccessModel model)
		{
			return new SqliteDatabaseCreator(this, model);
		}

		public override MigrationPlanApplicator NewMigrationPlanApplicator(DataAccessModel model)
		{
			throw new NotImplementedException();
		}

		public override MigrationPlanCreator NewMigrationPlanCreator(DataAccessModel model)
		{
			return new SqlDatabaseMigrationPlanCreator(this, model);
		}
        
		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(databaseTransactionContext);	
		}
	}
}
