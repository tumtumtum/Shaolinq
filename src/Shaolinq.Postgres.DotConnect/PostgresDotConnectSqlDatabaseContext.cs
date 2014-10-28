// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Transactions;
﻿using Shaolinq.Persistence;
using Devart.Data.PostgreSql;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres.DotConnect
{
    public class PostgresDotConnectSqlDatabaseContext
		: SqlDatabaseContext
    {
		public int Port { get; set; }
		public string Host { get; set; }
	    public string UserId { get; set; }
	    public string Password { get; set; }
	    
		public static PostgresDotConnectSqlDatabaseContext Create(PostgresDotConnectSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaults;
			var sqlDialect = PostgresSharedSqlDialect.Default;
			var sqlDataTypeProvider = new PostgresSharedSqlDataTypeProvider(constraintDefaults, contextInfo.NativeUuids, contextInfo.NativeEnums);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(sqlDialect, sqlDataTypeProvider, (options, sqlDataTypeProviderArg, sqlDialectArg) => new PostgresSharedSqlQueryFormatter(options, sqlDataTypeProviderArg, sqlDialectArg, contextInfo.SchemaName));

			return new PostgresDotConnectSqlDatabaseContext(model, sqlDialect, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		protected PostgresDotConnectSqlDatabaseContext(DataAccessModel model, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, PostgresDotConnectSqlDatabaseContextInfo contextInfo)
			: base(model, sqlDialect, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo.DatabaseName, contextInfo)
        {
            this.Host = contextInfo.ServerName;
            this.UserId = contextInfo.UserId;
            this.Password = contextInfo.Password;
			this.Port = contextInfo.Port;
			this.CommandTimeout = TimeSpan.FromSeconds(contextInfo.CommandTimeout);
			
            var connectionStringBuilder = new PgSqlConnectionStringBuilder
			{
				Host = contextInfo.ServerName,
				UserId = contextInfo.UserId,
				Password = contextInfo.Password,
				Port = contextInfo.Port,
				Pooling = contextInfo.Pooling,
				Enlist = false,
				ConnectionTimeout = contextInfo.ConnectionTimeout,
				Charset = "UTF8",
				Unicode = true,
				MaxPoolSize = contextInfo.MaxPoolSize,
				DefaultCommandTimeout = contextInfo.CommandTimeout,
				UnpreparedExecute = contextInfo.UnpreparedExecute
			};

            this.ServerConnectionString = connectionStringBuilder.ConnectionString;

			connectionStringBuilder.Database = contextInfo.DatabaseName;
            this.ConnectionString = connectionStringBuilder.ConnectionString;
			this.SchemaManager = new PostgresSharedSqlDatabaseSchemaManager(this);
        }

        public override SqlTransactionalCommandsContext CreateSqlTransactionalCommandsContext(Transaction transaction)
        {
            return new PostgresDotConnectSqlTransactionalCommandsContext(this, transaction);
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
					if (dataAccessObject.GetPrimaryKeysFlattened().Any(c => c.PersistedName == postgresException.ColumnName))
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
					if (dataAccessObject.GetPrimaryKeysFlattened().Any(c => Regex.Match(postgresException.DetailMessage, @"Key\s*\(\s*""?" + c.PersistedName + @"""?\s*\)", RegexOptions.CultureInvariant).Success))
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
