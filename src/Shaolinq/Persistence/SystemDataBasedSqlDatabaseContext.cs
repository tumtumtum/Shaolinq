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
		
		public virtual DbConnection OpenConnection()
		{
			var retval = this.dBProviderFactory.CreateConnection();
            
			retval.ConnectionString = this.GetConnectionString();
			retval.Open();
            
			return retval;
		}

		protected SystemDataBasedSqlDatabaseContext(SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, SqlDatabaseContextInfo contextInfo)
			: base(sqlDialect, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo)
		{
			this.CommandTimeout = TimeSpan.FromSeconds(contextInfo.CommandTimeout);
			this.ContextCategories = contextInfo.Categories == null ? new string[0] : contextInfo.Categories.Split(',').Select(c => c.Trim()).ToArray();
			this.dBProviderFactory = this.CreateDbProviderFactory();
		}

		public virtual string GetRelatedSql(Exception e)
		{
			return null;
		}

		public virtual Exception DecorateException(Exception exception, string relatedQuery)
		{
			return exception;
		}
		
		public abstract string GetConnectionString();
		public abstract DbProviderFactory CreateDbProviderFactory();
	}
}
