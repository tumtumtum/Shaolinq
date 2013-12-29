// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using Npgsql;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Sql;
 using Shaolinq.Persistence.Sql.Linq;
﻿using Shaolinq.Postgres.Shared;

﻿namespace Shaolinq.Postgres
{
	public class PostgresSqlDatabaseContext
		: SystemDataBasedDatabaseConnection
	{
		public string Host { get; set; }
		public string Userid { get; set; }
		public string Password { get; set; }
		public string Database { get; set; }
		public int Port { get; set; }

		public override string GetConnectionString()
		{
			return connectionString;
		}

		internal readonly string connectionString;
		internal readonly string databaselessConnectionString;

		public PostgresSqlDatabaseContext(string host, string userid, string password, string database, int port, bool pooling, int minPoolSize, int maxPoolSize, int connectionTimeoutSeconds, bool nativeUuids, int commandTimeoutSeconds, string schemaNamePrefix, DateTimeKind dateTimeKindIfUnspecifed)
			: base(database, PostgresSharedSqlDialect.Default, new PostgresSharedSqlDataTypeProvider(nativeUuids, dateTimeKindIfUnspecifed))
		{
			this.Host = host;
			this.Userid = userid;
			this.Password = password;
			this.Database = database;
			this.Port = port;
			this.SchemaNamePrefix = EnvironmentSubstitutor.Substitute(schemaNamePrefix);
			this.CommandTimeout = TimeSpan.FromSeconds(commandTimeoutSeconds);

			connectionString = String.Format("Host={0};User Id={1};Password={2};Database={3};Port={4};Pooling={5};MinPoolSize={6};MaxPoolSize={7};Enlist=false;Timeout={8};CommandTimeout={9}", host, userid, password, database, port, pooling, minPoolSize, maxPoolSize, connectionTimeoutSeconds, commandTimeoutSeconds);
			databaselessConnectionString = String.Format("Host={0};User Id={1};Password={2};Port={4};Pooling={5};MinPoolSize={6};MaxPoolSize={7};Enlist=false;Timeout={8};CommandTimeout={9}", host, userid, password, database, port, pooling, minPoolSize, maxPoolSize, connectionTimeoutSeconds, commandTimeoutSeconds);
		}

		public override DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			return new PostgresSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
		}

		public override Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
		{
			return new PostgresSharedSqlQueryFormatter(dataAccessModel, sqlDataTypeProvider, sqlDialect, expression, options);
		}

		public override DbProviderFactory NewDbProviderFactory()
		{
			return NpgsqlFactory.Instance;
		}

		public override DatabaseCreator NewDatabaseCreator(DataAccessModel model)
		{
			return new PostgresDatabaseCreator(this, model);
		}

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(databaseTransactionContext);	
		}

		public override void DropAllConnections()
		{
			NpgsqlConnection.ClearAllPools();
		}
	}
}
