// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Transactions;
﻿using Shaolinq.Persistence;
using MySql.Data.MySqlClient;

﻿namespace Shaolinq.MySql
{
	public class MySqlSqlDatabaseContext
		: SqlDatabaseContext
	{
		public string Username { get; private set; }
		public string Password { get; private set; }
		public string ServerName { get; private set; }
		
		public static MySqlSqlDatabaseContext Create(MySqlSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaults;
			var sqlDataTypeProvider = new MySqlSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(MySqlSqlDialect.Default, sqlDataTypeProvider, typeof(MySqlSqlQueryFormatter));

			return new MySqlSqlDatabaseContext(model, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		private MySqlSqlDatabaseContext(DataAccessModel model, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, MySqlSqlDatabaseContextInfo contextInfo)
			: base(model, MySqlSqlDialect.Default, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo.DatabaseName, contextInfo)
		{
			this.ServerName = contextInfo.ServerName;
			this.Username = contextInfo.UserName;
			this.Password = contextInfo.Password;

			this.ConnectionString = String.Format("Server={0}; Database={1}; Uid={2}; Pwd={3}; Pooling={4}; AutoEnlist=false; charset=utf8", this.ServerName, this.DatabaseName, this.Username, this.Password, contextInfo.PoolConnections);
			this.ServerConnectionString = String.Concat("Server=", this.ServerName, ";Database=mysql;AutoEnlist=false;Uid=", this.Username, ";Pwd=", this.Password);

			this.SchemaManager = new MySqlSqlDatabaseSchemaManager(this);
		}

		public override SqlTransactionalCommandsContext CreateSqlTransactionalCommandsContext(Transaction transaction)
		{
			return new DefaultSqlTransactionalCommandsContext(this, transaction);
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return new MySqlClientFactory();
		}

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlTransactionalCommandsContext sqlDatabaseCommandsContext)
		{
			return new DisabledForeignKeyCheckContext(sqlDatabaseCommandsContext);	
		}

		public override void DropAllConnections()
		{
		}

		public override Exception DecorateException(Exception exception, string relatedQuery)
		{
			var mySqlException = exception as MySqlException;

			if (mySqlException == null)
			{
				return base.DecorateException(exception, relatedQuery);
			}

			if (mySqlException.Number == 1062)
			{
				throw new UniqueKeyConstraintException(mySqlException, relatedQuery);
			}
			else
			{
				return new DataAccessException(exception, relatedQuery);
			}
		}
	}
}
