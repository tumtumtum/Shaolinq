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

		protected SystemDataBasedDatabaseConnection(string databaseName, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider)
		{
			this.CommandTimeout = TimeSpan.FromSeconds(60);

			this.SqlDialect = sqlDialect;
			this.SqlDataTypeProvider = sqlDataTypeProvider;

			this.DatabaseName = databaseName;

			this.dBProviderFactory = this.NewDbProviderFactory();
		}
		
		public abstract string GetConnectionString();
		public abstract DbProviderFactory NewDbProviderFactory();
	}
}
