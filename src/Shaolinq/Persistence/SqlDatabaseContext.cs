// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Transactions;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence
{
	public abstract class SqlDatabaseContext
		: IDisposable
	{
		public TimeSpan CommandTimeout { get; protected set; }


		private readonly DbProviderFactory dBProviderFactory;
		internal volatile Dictionary<SqlQueryProvider.ProjectorCacheKey, SqlQueryProvider.ProjectorCacheInfo> projectorCache = new Dictionary<SqlQueryProvider.ProjectorCacheKey, SqlQueryProvider.ProjectorCacheInfo>(SqlQueryProvider.ProjectorCacheEqualityComparer.Default); 
		internal volatile Dictionary<DefaultSqlDatabaseTransactionContext.SqlCommandKey, DefaultSqlDatabaseTransactionContext.SqlCommandValue> formattedInsertSqlCache = new Dictionary<DefaultSqlDatabaseTransactionContext.SqlCommandKey, DefaultSqlDatabaseTransactionContext.SqlCommandValue>(DefaultSqlDatabaseTransactionContext.CommandKeyComparer.Default);
		internal volatile Dictionary<DefaultSqlDatabaseTransactionContext.SqlCommandKey, DefaultSqlDatabaseTransactionContext.SqlCommandValue> formattedUpdateSqlCache = new Dictionary<DefaultSqlDatabaseTransactionContext.SqlCommandKey, DefaultSqlDatabaseTransactionContext.SqlCommandValue>(DefaultSqlDatabaseTransactionContext.CommandKeyComparer.Default);

		public string DatabaseName { get; private set; }
		public string SchemaName { get; protected set; }
		public string[] ContextCategories { get; protected set; }
		public string TableNamePrefix { get; protected set; }
		public DataAccessModel DataAccessModel { get; private set; }
		public SqlDialect SqlDialect { get; protected set; }
		public SqlDataTypeProvider SqlDataTypeProvider { get; protected set; }
		public SqlDatabaseSchemaManager SchemaManager { get; protected set; }
		public SqlQueryFormatterManager SqlQueryFormatterManager { get; protected set; }
		public string ConnectionString { get; protected set; }
		public string ServerConnectionString { get; protected set; }
		
		public abstract DbProviderFactory CreateDbProviderFactory();
		public abstract SqlDatabaseTransactionContext CreateDatabaseTransactionContext(Transaction transaction);
		public abstract IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlDatabaseTransactionContext sqlDatabaseTransactionContext);

		public virtual DbConnection OpenConnection()
		{
			var retval = this.dBProviderFactory.CreateConnection();

			retval.ConnectionString = this.ConnectionString;
			retval.Open();

			return retval;
		}

		protected SqlDatabaseContext(DataAccessModel model, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, string databaseName, SqlDatabaseContextInfo contextInfo)
		{
			this.DatabaseName = databaseName;
			this.DataAccessModel = model; 
			this.CommandTimeout = TimeSpan.FromSeconds(contextInfo.CommandTimeout);
			this.ContextCategories = contextInfo.Categories == null ? new string[0] : contextInfo.Categories.Split(',').Select(c => c.Trim()).ToArray();
			this.dBProviderFactory = this.CreateDbProviderFactory();
			this.SqlDialect = sqlDialect;
			this.SqlDataTypeProvider = sqlDataTypeProvider;
			this.SqlQueryFormatterManager = sqlQueryFormatterManager;
			this.SchemaName = EnvironmentSubstitutor.Substitute(contextInfo.SchemaName);
			this.TableNamePrefix = EnvironmentSubstitutor.Substitute(contextInfo.TableNamePrefix);
		}

		public virtual IPersistenceQueryProvider CreateQueryProvider()
		{
			return new SqlQueryProvider(this.DataAccessModel, this);
		}
		
		public virtual string GetRelatedSql(Exception e)
		{
			return null;
		}

		public virtual Exception DecorateException(Exception exception, string relatedQuery)
		{
			return exception;
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
