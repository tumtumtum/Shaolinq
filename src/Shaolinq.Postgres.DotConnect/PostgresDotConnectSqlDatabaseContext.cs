// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using Devart.Data.PostgreSql;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.DotConnect
{
    using System.Data;

    public class PostgresDotConnectSqlDatabaseContext
		: SqlDatabaseContext
	{
		public int Port { get; set; }
		public string Host { get; set; }
		public string UserId { get; set; }
		public string Password { get; set; }
		
		public static PostgresDotConnectSqlDatabaseContext Create(PostgresDotConnectSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaultsConfiguration;
			var sqlDialect = new PostgresDotConnectSqlDialect();
			var sqlDataTypeProvider = new PostgresSqlDataTypeProvider(model.TypeDescriptorProvider, constraintDefaults, contextInfo.NativeUuids, contextInfo.NativeEnums);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(sqlDialect, sqlDataTypeProvider, (options, sqlDataTypeProviderArg, sqlDialectArg) => new PostgresDotConnectSqlQueryFormatter(options, sqlDataTypeProviderArg, sqlDialectArg, contextInfo.SchemaName, false));

			return new PostgresDotConnectSqlDatabaseContext(model, sqlDialect, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		protected PostgresDotConnectSqlDatabaseContext(DataAccessModel model, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, PostgresDotConnectSqlDatabaseContextInfo contextInfo)
			: base(model, sqlDialect, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo.DatabaseName, contextInfo)
		{
			if (!string.IsNullOrEmpty(contextInfo.ConnectionString))
			{
				this.ConnectionString = contextInfo.ConnectionString;
				this.ServerConnectionString = Regex.Replace(this.ConnectionString, @"Database\s*\=[^;$]+[;$]", "");
			}
			else
			{
				this.Host = contextInfo.ServerName;
				this.UserId = contextInfo.UserId;
				this.Password = contextInfo.Password;
				this.Port = contextInfo.Port;
				
				var connectionStringBuilder = new PgSqlConnectionStringBuilder
				{
					Host = contextInfo.ServerName,
					UserId = contextInfo.UserId,
					Password = contextInfo.Password,
					Port = contextInfo.Port,
					Pooling = contextInfo.Pooling,
					Enlist = false,
					Charset = "UTF8",
					Unicode = true,
					MaxPoolSize = contextInfo.MaxPoolSize,
					UnpreparedExecute = contextInfo.UnpreparedExecute
				};

				if (contextInfo.ConnectionTimeout != null)
				{
					connectionStringBuilder.ConnectionTimeout = contextInfo.ConnectionTimeout.Value;
				}

				if (contextInfo.ConnectionCommandTimeout != null)
				{
					connectionStringBuilder.DefaultCommandTimeout = contextInfo.ConnectionCommandTimeout.Value;
				}

				this.ServerConnectionString = connectionStringBuilder.ConnectionString;
				connectionStringBuilder.Database = contextInfo.DatabaseName;
				this.ConnectionString = connectionStringBuilder.ConnectionString;
			}

			this.SchemaManager = new PostgresSqlDatabaseSchemaManager(this);
		}

        protected override SqlTransactionalCommandsContext CreateSqlTransactionalCommandsContext(IDbConnection connection, DataAccessTransaction transaction)
        {
			return new PostgresDotConnectSqlTransactionalCommandsContext(this, connection, transaction);
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return PgSqlProviderFactory.Instance;
		}
		
		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlTransactionalCommandsContext sqlDatabaseCommandsContext)
		{
			return new DisabledForeignKeyCheckContext(sqlDatabaseCommandsContext);	
		}

		public override void DropAllConnections()
		{
			PgSqlConnection.ClearAllPools();
		}

		public override Exception DecorateException(Exception exception, DataAccessObject dataAccessObject, string relatedQuery)
		{
			var postgresException = exception as PgSqlException;

			if (postgresException == null)
			{
				return base.DecorateException(exception, dataAccessObject, relatedQuery);
			}

			switch (postgresException.ErrorCode)
			{
			case "40001":
				return new ConcurrencyException(exception, relatedQuery);
			case "23502":
				return new MissingPropertyValueException(dataAccessObject, postgresException, relatedQuery);
			case "23503":
				return new MissingRelatedDataAccessObjectException(null, dataAccessObject, postgresException, relatedQuery);
			case "23505":
				if (!string.IsNullOrEmpty(postgresException.ColumnName) && dataAccessObject != null)
				{
					if (dataAccessObject.GetAdvanced().GetPrimaryKeysFlattened().Any(c => c.PersistedName == postgresException.ColumnName))
					{
						return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);
					}
				}

				if (!string.IsNullOrEmpty(postgresException.ConstraintName) && postgresException.ConstraintName.EndsWith("_pkey"))
				{
					return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);
				}

				if (!string.IsNullOrEmpty(postgresException.DetailMessage) && dataAccessObject != null)
				{
					if (dataAccessObject.GetAdvanced().GetPrimaryKeysFlattened().Any(c => Regex.Match(postgresException.DetailMessage, @"Key\s*\(\s*""?" + c.PersistedName + @"""?\s*\)", RegexOptions.CultureInvariant).Success))
					{
						return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);	
					}
				}

				if (postgresException.Message.IndexOf("_pkey", StringComparison.InvariantCultureIgnoreCase) >= 0)
				{
					return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);	
				}
				else
				{
					return new UniqueConstraintException(exception, relatedQuery);
				}
			}

			return new DataAccessException(exception, relatedQuery);
		}
	}
}
