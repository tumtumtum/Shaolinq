// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Transactions;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence
{
	public abstract class SqlDatabaseContext
		: IDisposable
	{
		internal volatile Dictionary<SqlQueryProvider.ProjectorCacheKey, SqlQueryProvider.ProjectorCacheInfo> projectorCache = new Dictionary<SqlQueryProvider.ProjectorCacheKey, SqlQueryProvider.ProjectorCacheInfo>(SqlQueryProvider.ProjectorCacheEqualityComparer.Default); 
		internal volatile Dictionary<SqlDatabaseTransactionContext.SqlCommandKey, SqlDatabaseTransactionContext.SqlCommandValue> formattedInsertSqlCache = new Dictionary<SqlDatabaseTransactionContext.SqlCommandKey, SqlDatabaseTransactionContext.SqlCommandValue>(SqlDatabaseTransactionContext.CommandKeyComparer.Default);
		internal volatile Dictionary<SqlDatabaseTransactionContext.SqlCommandKey, SqlDatabaseTransactionContext.SqlCommandValue> formattedUpdateSqlCache = new Dictionary<SqlDatabaseTransactionContext.SqlCommandKey, SqlDatabaseTransactionContext.SqlCommandValue>(SqlDatabaseTransactionContext.CommandKeyComparer.Default);
		
		public string DatabaseName { get; protected set; }
		public string SchemaName { get; protected set; }
		public string[] ContextCategories { get; protected set; }
		public string TableNamePrefix { get; protected set; }
		public SqlDialect SqlDialect { get; protected set; }
		public SqlDataTypeProvider SqlDataTypeProvider { get; protected set; }
		
		public abstract Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options);
		public abstract DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction);
		public abstract DatabaseCreator NewDatabaseCreator(DataAccessModel model);
		public abstract IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext);
		
		public virtual IPersistenceQueryProvider NewQueryProvider(DataAccessModel dataAccessModel)
		{
			return new SqlQueryProvider(dataAccessModel, this);
		}

		public virtual void DropAllConnections()
		{
		}

		public virtual void Dispose()
		{
			DropAllConnections();
		}
	}
}
