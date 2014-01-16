// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteOfficialsSqlDatabaseContext
		: SqliteSqlDatabaseContext
	{
		public static SqliteSqlDatabaseContext Create(SqliteSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaults;
			var sqlDataTypeProvider = new SqliteSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(SqliteSqlDialect.Default, sqlDataTypeProvider, typeof(SqliteSqlQueryFormatter));

			return new SqliteOfficialsSqlDatabaseContext(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager);
		}

		public SqliteOfficialsSqlDatabaseContext(DataAccessModel model, SqliteSqlDatabaseContextInfo contextInfo, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager)
			: base(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager)
		{
			this.ConnectionString = SqliteRuntimeOfficialAssemblyManager.BuildConnectionString(contextInfo.FileName);
			this.ServerConnectionString = this.ConnectionString;
			this.SchemaManager = new SqliteOfficialSqlDatabaseSchemaManager(this);
		}

		public override void DropAllConnections()
		{
			SqliteRuntimeOfficialAssemblyManager.ClearAllPools();
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return SqliteRuntimeOfficialAssemblyManager.NewDbProviderFactory();
		}

		public override Exception DecorateException(Exception exception, string relatedQuery)
		{
			// http://www.sqlite.org/c3ref/c_abort.html

			if (!SqliteRuntimeOfficialAssemblyManager.IsSqLiteExceptionType(exception))
			{	
				return base.DecorateException(exception, relatedQuery);
			}

			if (SqliteRuntimeOfficialAssemblyManager.GetExceptionErrorCode(exception) == SqliteErrorCodes.SqliteConstraint)
			{
				return new UniqueKeyConstraintException(exception, relatedQuery);
			}

			return new DataAccessException(exception, relatedQuery);
		}
	}
}
