using System;
using System.Data.Common;

namespace Shaolinq.Persistence.Sql
{
	public abstract class SqlPersistenceContext
		: PersistenceContext
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

		protected SqlPersistenceContext(string persistenceStoreName, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider)
		{
			this.CommandTimeout = TimeSpan.FromSeconds(60);

			this.SqlDialect = sqlDialect;
			this.SqlDataTypeProvider = sqlDataTypeProvider;

			this.PersistenceStoreName = persistenceStoreName;

			this.dBProviderFactory = NewDbProproviderFactory();
		}

		/// <summary>
		/// Gets the ODBC/ADO connection string for the current database
		/// </summary>
		/// <returns>
		/// A database connection string
		/// </returns>
		protected abstract string GetConnectionString();

		/// <summary>
		/// Creates a new <see cref="DbProviderFactory"/>
		/// </summary>
		/// <returns>
		/// The new <see cref="DbProviderFactory"/>
		/// </returns>
		protected abstract DbProviderFactory NewDbProproviderFactory();

		public abstract TableDescriptor GetTableDescriptor(string tableName);
        
		public abstract SqlSchemaWriter NewSqlSchemaWriter(BaseDataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo);

		public abstract IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(PersistenceTransactionContext persistenceTransactionContext);
	}
}
