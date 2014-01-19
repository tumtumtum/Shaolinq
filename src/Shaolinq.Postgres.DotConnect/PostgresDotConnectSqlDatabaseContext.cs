// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
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
	    
		public static PostgresDotConnectSqlDatabaseContext Create(PostgresDotConnectSqlSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaults;
			var sqlDialect = PostgresSharedSqlDialect.Default;
			var sqlDataTypeProvider = new PostgresSharedSqlDataTypeProvider(constraintDefaults, contextInfo.NativeUuids, contextInfo.NativeEnums, contextInfo.DateTimeKindIfUnspecified);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(sqlDialect, sqlDataTypeProvider, (options, sqlDataTypeProviderArg, sqlDialectArg) => new PostgresSharedSqlQueryFormatter(options, sqlDataTypeProviderArg, sqlDialectArg, contextInfo.SchemaName));

			return new PostgresDotConnectSqlDatabaseContext(model, sqlDialect, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		protected PostgresDotConnectSqlDatabaseContext(DataAccessModel model, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, PostgresDotConnectSqlSqlDatabaseContextInfo contextInfo)
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
				DefaultCommandTimeout = contextInfo.CommandTimeout
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

		public override Exception DecorateException(Exception exception, string relatedQuery)
		{
			var postgresException = exception as PgSqlException;

			if (postgresException == null)
			{
				return base.DecorateException(exception, relatedQuery);
			}

			if (postgresException.ErrorCode == "40001")
			{
				return new ConcurrencyException(exception, relatedQuery);
			}
			else if (postgresException.ErrorCode == "23505")
			{
				throw new UniqueKeyConstraintException(exception, relatedQuery);
			}

			return new DataAccessException(exception, relatedQuery);
		}
    }
}
