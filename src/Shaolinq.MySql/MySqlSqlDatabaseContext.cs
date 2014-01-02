// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Transactions;
﻿using Shaolinq.Persistence;
using MySql.Data.MySqlClient;

﻿namespace Shaolinq.MySql
{
	public class MySqlSqlDatabaseContext
		: SystemDataBasedSqlDatabaseContext
	{
		public string DatabaseName { get; protected set; }
		public string ServerName { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }

		public override string GetConnectionString()
		{
			return connectionString;
		}

		internal readonly string connectionString;
		internal readonly string databaselessConnectionString;

		public MySqlSqlDatabaseContext(MySqlSqlDatabaseContextInfo contextInfo)
			: base(MySqlSqlDialect.Default, MySqlSqlDataTypeProvider.Instance, new DefaultSqlQueryFormatterManager(MySqlSqlDialect.Default, MySqlSqlDataTypeProvider.Instance, typeof(MySqlSqlQueryFormatter)), contextInfo)
		{
			this.ServerName = contextInfo.ServerName;
			this.Username = contextInfo.UserName;
			this.Password = contextInfo.Password;
			this.DatabaseName = contextInfo.DatabaseName;

			connectionString = String.Format("Server={0}; Database={1}; Uid={2}; Pwd={3}; Pooling={4}; charset=utf8", this.ServerName, this.DatabaseName, this.Username, this.Password, contextInfo.PoolConnections);
			databaselessConnectionString = String.Concat("Server=", this.ServerName, ";Database=mysql;Uid=", this.Username, ";Pwd=", this.Password);
		}

		public override DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			return new MySqlSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
		}

		public override DbProviderFactory CreateDbProviderFactory()
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
