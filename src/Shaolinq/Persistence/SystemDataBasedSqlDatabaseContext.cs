// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Linq;

namespace Shaolinq.Persistence
{
	public abstract class SystemDataBasedSqlDatabaseContext
		: SqlDatabaseContext
	{
		private readonly DbProviderFactory dBProviderFactory;

		public TimeSpan CommandTimeout { get; set; }
		
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

		protected SystemDataBasedSqlDatabaseContext(string databaseName, string schemaName, string tableNamePrefix, string categories, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider)
		{
			this.CommandTimeout = TimeSpan.FromSeconds(60);

			this.SqlDialect = sqlDialect;
			this.SqlDataTypeProvider = sqlDataTypeProvider;

			this.DatabaseName = databaseName;
			this.ContextCategories = categories == null ? new string[0] : categories.Split(',').Select(c => c.Trim()).ToArray();

			this.SchemaName = EnvironmentSubstitutor.Substitute(schemaName);
			this.TableNamePrefix = EnvironmentSubstitutor.Substitute(tableNamePrefix);

			this.dBProviderFactory = this.NewDbProviderFactory();
		}
		
		public abstract string GetConnectionString();
		public abstract DbProviderFactory NewDbProviderFactory();
	}
}
