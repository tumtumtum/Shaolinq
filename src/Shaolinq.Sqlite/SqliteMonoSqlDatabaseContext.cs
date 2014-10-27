// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Linq;
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
			var connectionStringBuilder = new SqliteConnectionStringBuilder
			{
				Enlist = false,
				DataSource = contextInfo.FileName
			};

			connectionStringBuilder.Add("foreign keys", 1);

			this.ConnectionString = connectionStringBuilder.ConnectionString;
			this.ServerConnectionString = this.ConnectionString;

			this.SchemaManager = new SqliteMonoSqlDatabaseSchemaManager(this);
		}

		public override void DropAllConnections()
		{
			SqliteConnection.ClearAllPools();
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return new SqliteFactory();
		}

		public override Exception DecorateException(Exception exception, IDataAccessObject dataAccessObject, string relatedQuery)
		{
			// http://www.sqlite.org/c3ref/c_abort.html

			var sqliteException = exception as SqliteException;

			if (sqliteException == null)
			{
				return base.DecorateException(exception, dataAccessObject, relatedQuery);
			}

			if (sqliteException.ErrorCode == SQLiteErrorCode.Constraint)
			{
				if (sqliteException.Message.IndexOf("FOREIGN KEY", System.StringComparison.Ordinal) >= 0)
				{
					return new MissingRelatedDataAccessObjectException(null, dataAccessObject, sqliteException, relatedQuery);
				}
				else if (sqliteException.Message.IndexOf("NOT NULL", StringComparison.Ordinal) >= 0)
				{
					return new MissingPropertyValueException(dataAccessObject, sqliteException, relatedQuery);
				}
				else
				{
					if (dataAccessObject != null)
					{
						var primaryKeyNames = dataAccessObject.TypeDescriptor.PrimaryKeyProperties.Select(c => c.DeclaringTypeDescriptor.PersistedName + "." + c.PersistedName);

						if (primaryKeyNames.Any(c => sqliteException.Message.IndexOf(c, StringComparison.Ordinal) >= 0))
						{
							return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);
						}
					}

					return new UniqueConstraintException(exception, relatedQuery);
				}
			}

			return new DataAccessException(exception, relatedQuery);
		}
	}
}
