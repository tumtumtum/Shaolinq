// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

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

		public virtual bool SupportsNestedReaders
		{
			get
			{
				return false;
			}
		}

		public abstract Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options);
		public abstract IPersistenceQueryProvider NewQueryProvider(DataAccessModel dataAccessModel, DatabaseConnection databaseConnection);
		public abstract DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction);
		public abstract DatabaseCreator NewDatabaseCreator(DataAccessModel model);
		public abstract MigrationPlanApplicator NewMigrationPlanApplicator(DataAccessModel model);
		public abstract MigrationPlanCreator NewMigrationPlanCreator(DataAccessModel model);

		public abstract void DropAllConnections();
		
		public virtual void Dispose()
		{
			DropAllConnections();
		}
	}
}
