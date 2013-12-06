// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Data.Common;

namespace Shaolinq.Persistence.Sql
{
	public abstract class SystemDataBasedDatabaseConnection
		: DatabaseConnection
	{
		private readonly DbProviderFactory dBProviderFactory;

		public TimeSpan CommandTimeout { get; set; }
		public abstract bool SupportsNestedTransactions { get; }
		public abstract bool SupportsDisabledForeignKeyCheckContext { get; }

		public abstract bool CreateDatabase(bool overwrite);

		/// <summary>
		/// Opens a new <see cref="DbConnection"/>
		/// </summary>
		/// <returns>
		/// The new <see cref="DbConnection"/>
		/// </returns>
		public virtual DbConnection OpenConnection()
		{
			var retval = this.dBProviderFactory.CreateConnection();
            
			retval.ConnectionString = this.GetConnectionString();
			retval.Open();
            
			return retval;
		}

		protected SystemDataBasedDatabaseConnection(string persistenceStoreName, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider)
		{
			this.CommandTimeout = TimeSpan.FromSeconds(60);

			this.SqlDialect = sqlDialect;
			this.SqlDataTypeProvider = sqlDataTypeProvider;

			this.PersistenceStoreName = persistenceStoreName;

			this.dBProviderFactory = NewDbProproviderFactory();
		}

		protected abstract string GetConnectionString();
		protected abstract DbProviderFactory NewDbProproviderFactory();
		public abstract TableDescriptor GetTableDescriptor(string tableName);
        public abstract SqlSchemaWriter NewSqlSchemaWriter(DataAccessModel model);
		public abstract IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext);
	}
}
