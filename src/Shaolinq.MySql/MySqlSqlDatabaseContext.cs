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
		public string DatabaseName { get; protected set; }
		public string ServerName { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }

		public static MySqlSqlDatabaseContext Create(MySqlSqlDatabaseContextInfo contextInfo, ConstraintDefaults constraintDefaults)
		{
			var sqlDataTypeProvider = new MySqlSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(MySqlSqlDialect.Default, sqlDataTypeProvider, typeof(MySqlSqlQueryFormatter));

			return new MySqlSqlDatabaseContext(sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		private MySqlSqlDatabaseContext(SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, MySqlSqlDatabaseContextInfo contextInfo)
			: base(MySqlSqlDialect.Default, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo)
		{
			this.ServerName = contextInfo.ServerName;
			this.Username = contextInfo.UserName;
			this.Password = contextInfo.Password;
			this.DatabaseName = contextInfo.DatabaseName;

			this.ConnectionString = String.Format("Server={0}; Database={1}; Uid={2}; Pwd={3}; Pooling={4}; charset=utf8", this.ServerName, this.DatabaseName, this.Username, this.Password, contextInfo.PoolConnections);
			this.ServerConnectionString = String.Concat("Server=", this.ServerName, ";Database=mysql;Uid=", this.Username, ";Pwd=", this.Password);
		}

		public override SqlDatabaseTransactionContext CreateDatabaseTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			return new DefaultSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return new MySqlClientFactory();
		}

		public override DatabaseCreator CreateDatabaseCreator(DataAccessModel model)
		{
			return new MySqlDatabaseCreator(this, model);
		}

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlDatabaseTransactionContext sqlDatabaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(sqlDatabaseTransactionContext);	
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
