// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Linq.Expressions;
using System.Transactions;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Linq;
using MySql.Data.MySqlClient;

﻿namespace Shaolinq.MySql
{
	public class MySqlSqlDatabaseContext
		: SystemDataBasedSqlDatabaseContext
	{
		public string ServerName { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }

		public override string GetConnectionString()
		{
			return connectionString;
		}

		internal readonly string connectionString;
		internal readonly string databaselessConnectionString;

		public MySqlSqlDatabaseContext(string serverName, string database, string username, string password, bool poolConnections, string schemaName, string tableNamePrefix, string categories)
			: base(database, schemaName, tableNamePrefix, categories, MySqlSqlDialect.Default, MySqlSqlDataTypeProvider.Instance)
		{
			this.ServerName = serverName;
			this.Username = username;
			this.Password = password;
			
			connectionString = String.Format("Server={0}; Database={1}; Uid={2}; Pwd={3}; Pooling={4}; charset=utf8", this.ServerName, this.DatabaseName, this.Username, this.Password, poolConnections);
			databaselessConnectionString = String.Concat("Server=", this.ServerName, ";Database=mysql;Uid=", this.Username, ";Pwd=", this.Password);
		}

		public override DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			return new MySqlSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
		}

		public override Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
		{
			return new MySqlSqlQueryFormatter(dataAccessModel, sqlDataTypeProvider, sqlDialect, expression, options);
		}

		public override DbProviderFactory NewDbProviderFactory()
		{
			return new MySqlClientFactory();
		}

		public override DatabaseCreator NewDatabaseCreator(DataAccessModel model)
		{
			return new MySqlDatabaseCreator(this, model);
		}

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(databaseTransactionContext);	
		}

		public override void DropAllConnections()
		{
		}
	}
}
