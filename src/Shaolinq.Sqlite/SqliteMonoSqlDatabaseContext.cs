// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using Mono.Data.Sqlite;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteMonoSqlDatabaseContext
		: SqliteSqlDatabaseContext
	{
		public static SqliteSqlDatabaseContext Create(SqliteSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaults;
			var sqlDataTypeProvider = new SqliteSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(SqliteSqlDialect.Default, sqlDataTypeProvider, typeof(SqliteSqlQueryFormatter));

			return new SqliteMonoSqlDatabaseContext(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager);
		}

		public SqliteMonoSqlDatabaseContext(DataAccessModel model, SqliteSqlDatabaseContextInfo contextInfo, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager)
			: base(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager)
		{
		}

		public override void DropAllConnections()
		{
			SqliteConnection.ClearAllPools();
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return new SqliteFactory();
		}

		public override Exception DecorateException(Exception exception, string relatedQuery)
		{
			// http://www.sqlite.org/c3ref/c_abort.html

			var sqliteException = exception as SqliteException;

			if (sqliteException == null)
			{
				return base.DecorateException(exception, relatedQuery);
			}

			if (sqliteException.ErrorCode == SQLiteErrorCode.Constraint)
			{
				return new UniqueKeyConstraintException(exception, relatedQuery);
			}

			return new DataAccessException(exception, relatedQuery);
		}
	}
}
