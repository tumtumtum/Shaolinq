// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
 using System.Data.Common;
 using System.Linq.Expressions;
using System.Transactions;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Linq;
using Devart.Data.PostgreSql;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres.DotConnect
{
    public class PostgresDotConnectSqlDatabaseContext
        : SystemDataBasedSqlDatabaseContext
    {
		public int Port { get; set; }
		public string Host { get; set; }
	    public string Userid { get; set; }
	    public string Password { get; set; }
	    public string Database { get; set; }
	    internal readonly string connectionString;
		internal readonly string databaselessConnectionString;

		public override string GetConnectionString()
        {
            return connectionString;
        }

        public PostgresDotConnectSqlDatabaseContext(string host, string userid, string password, string database, int port, bool pooling, int minPoolSize, int maxPoolSize, int connectionTimeoutSeconds, int commandTimeoutSeconds, bool nativeUuids, string schemaName, string tableNamePrefix, string categories, DateTimeKind dateTimeKindForUnspecifiedDateTimeKinds)
			: base(database, schemaName, tableNamePrefix, categories, PostgresSharedSqlDialect.Default, new PostgresSharedSqlDataTypeProvider(nativeUuids, dateTimeKindForUnspecifiedDateTimeKinds))
        {
            this.Host = host;
            this.Userid = userid;
            this.Password = password;
            this.Database = database;
            this.Port = port; 
            this.CommandTimeout = TimeSpan.FromSeconds(commandTimeoutSeconds);
			
            var sb = new PgSqlConnectionStringBuilder
			{
				Host = host,
				UserId = userid,
				Password = password,
				Port = port,
				Pooling = pooling,
				Enlist = false,
				ConnectionTimeout = connectionTimeoutSeconds,
				Charset = "UTF8",
				Unicode = true,
				MaxPoolSize = maxPoolSize,
				DefaultCommandTimeout = commandTimeoutSeconds
			};

            databaselessConnectionString = sb.ConnectionString;
            
            sb.Database = database;
            connectionString = sb.ConnectionString;
        }

        public override DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
        {
            return new PostgresDotConnectSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
        }

		public override Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
        {
            return new PostgresSharedSqlQueryFormatter(dataAccessModel, this.SchemaName, sqlDataTypeProvider, sqlDialect, expression, options);
        }

		public override DbProviderFactory NewDbProviderFactory()
        {
            return PgSqlProviderFactory.Instance;
        }
		
	    public override DatabaseCreator NewDatabaseCreator(DataAccessModel model)
	    {
		    return new PostgresDotConnectDatabaseCreator(this, model);
	    }

        public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext)
        {
            return new DisabledForeignKeyCheckContext(databaseTransactionContext);	
        }

		public override void DropAllConnections()
		{
			PgSqlConnection.ClearAllPools();
		}
    }
}
