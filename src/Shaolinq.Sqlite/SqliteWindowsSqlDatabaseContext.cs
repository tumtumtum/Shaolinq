// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Data.SQLite;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteWindowsSqlDatabaseContext
		: SqliteSqlDatabaseContext
	{
		public static SqliteSqlDatabaseContext Create(SqliteSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaults;
			var sqlDataTypeProvider = new SqliteSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(SqliteSqlDialect.Default, sqlDataTypeProvider, typeof(SqliteSqlQueryFormatter));

			return new SqliteWindowsSqlDatabaseContext(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager);
		}

		public SqliteWindowsSqlDatabaseContext(DataAccessModel model, SqliteSqlDatabaseContextInfo contextInfo, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager)
			: base(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager)
		{
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return new SQLiteFactory();
		}

		public override Exception DecorateException(Exception exception, string relatedQuery)
		{
			// http://www.sqlite.org/c3ref/c_abort.html

			var sqliteException = exception as SQLiteException;

			if (sqliteException == null)
			{
				return base.DecorateException(exception, relatedQuery);
			}

			if (sqliteException.ErrorCode == SqliteErrorCodes.SqliteConstraint)
			{
				return new UniqueKeyConstraintException(exception, relatedQuery);
			}

			return new DataAccessException(exception, relatedQuery);
		}
	}
}
