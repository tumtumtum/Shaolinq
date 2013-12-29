﻿// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Transactions;
using Shaolinq.Persistence.Sql;
using Shaolinq.Persistence.Sql.Linq;

namespace Shaolinq.Persistence
{
	public abstract class DatabaseConnection
		: IDisposable
	{
		public string DatabaseName { get; set; }
		public SqlDialect SqlDialect { get; protected set; }
		public string SchemaNamePrefix { get; protected set; }
		public SqlDataTypeProvider SqlDataTypeProvider { get; protected set; }
		
		public abstract bool CreateDatabase(bool overwrite);
		public abstract Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options);
		public abstract DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction);
		public abstract DatabaseCreator NewDatabaseCreator(DataAccessModel model);
		public abstract MigrationPlanApplicator NewMigrationPlanApplicator(DataAccessModel model);
		public abstract MigrationPlanCreator NewMigrationPlanCreator(DataAccessModel model);
		public abstract TableDescriptor GetTableDescriptor(string tableName);
		public abstract SqlSchemaWriter NewSqlSchemaWriter(DataAccessModel model);
		public abstract IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext);
		public abstract void DropAllConnections();

		public virtual IPersistenceQueryProvider NewQueryProvider(DataAccessModel dataAccessModel)
		{
			return new SqlQueryProvider(dataAccessModel, this);
		}
		
		public virtual void Dispose()
		{
			DropAllConnections();
		}
	}
}