// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
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
		
		public string SchemaName { get; protected set; }
		public string[] ContextCategories { get; protected set; }
		public string TableNamePrefix { get; protected set; }
		public SqlDialect SqlDialect { get; protected set; }
		public SqlDataTypeProvider SqlDataTypeProvider { get; protected set; }
		public SqlQueryFormatterManager SqlQueryFormatterManager { get; protected set; }

		public abstract DatabaseTransactionContext CreateDatabaseTransactionContext(DataAccessModel dataAccessModel, Transaction transaction);
		public abstract DatabaseCreator CreateDatabaseCreator(DataAccessModel model);
		public abstract IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext);

		protected SqlDatabaseContext(SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, SqlDatabaseContextInfo contextInfo)
		{
			this.SqlDialect = sqlDialect;
			this.SqlDataTypeProvider = sqlDataTypeProvider;
			this.SqlQueryFormatterManager = sqlQueryFormatterManager;
			this.SchemaName = EnvironmentSubstitutor.Substitute(contextInfo.SchemaName);
			this.TableNamePrefix = EnvironmentSubstitutor.Substitute(contextInfo.TableNamePrefix);
		}

		public virtual IPersistenceQueryProvider CreateQueryProvider(DataAccessModel dataAccessModel)
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
