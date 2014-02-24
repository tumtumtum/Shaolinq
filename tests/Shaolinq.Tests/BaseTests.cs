// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
﻿using System.Reflection;
 using NUnit.Framework;
 using Shaolinq.MySql;
﻿using Shaolinq.Postgres;
﻿using Shaolinq.Postgres.DotConnect;
﻿using Shaolinq.Sqlite;
using Shaolinq.Tests.TestModel;
using log4net.Config;

namespace Shaolinq.Tests
{
	public class BaseTests
	{
		protected TestDataAccessModel model;

		protected DataAccessModelConfiguration CreateMySqlConfiguration(string databaseName)
		{
			return MySqlConfiguration.Create(databaseName, "localhost", "root", "root");
		}

		protected DataAccessModelConfiguration CreateSqliteConfiguration(string databaseName)
		{
			return SqliteConfiguration.Create(databaseName + ".db");
		}

		protected DataAccessModelConfiguration CreateSqliteInMemoryConfiguration(string databaseName)
		{
			return SqliteConfiguration.Create("file:" + databaseName + "?mode=memory&cache=shared");
		}

		protected DataAccessModelConfiguration CreateSqliteClassicInMemoryConfiguration(string databaseName)
		{
			return SqliteConfiguration.Create(":memory:");	
		}

		protected DataAccessModelConfiguration CreatePostgresConfiguration(string databaseName)
		{
			return PostgresConfiguration.Create(new PostgresSqlDatabaseContextInfo()
			{
				DatabaseName = databaseName,
				ServerName = "localhost",
				UserId = "postgres",
				Password = "postgres",
				NativeEnums = true,
				Categories = null
			});
		}

		protected DataAccessModelConfiguration CreatePostgresDotConnectConfiguration(string databaseName)
		{
			return PostgresDotConnectConfiguration.Create(new PostgresDotConnectSqlSqlDatabaseContextInfo
			{
				DatabaseName = "DotConnect" + databaseName,
				ServerName = "localhost",
				UserId = "postgres",
				Password = "postgres",
				Categories = null,
				UnpreparedExecute = false
			});
		}

		protected DataAccessModelConfiguration CreatePostgresDotConnectUnpreparedConfiguration(string databaseName)
		{
			return PostgresDotConnectConfiguration.Create(new PostgresDotConnectSqlSqlDatabaseContextInfo
			{
				DatabaseName = "DotConnectUnprepared" + databaseName,
				ServerName = "localhost",
				UserId = "postgres",
				Password = "postgres",
				Categories = null,
				UnpreparedExecute = true
			});
		}

		protected DataAccessModelConfiguration Create(string providerName, string databaseName)
		{
			var methodInfo = this.GetType().GetMethod("Create" + providerName.Replace(".", "") + "Configuration", BindingFlags.Instance | BindingFlags.NonPublic);

			return (DataAccessModelConfiguration)methodInfo.Invoke(this, new object[] { databaseName });
		}

		protected string ProviderName
		{
			get;
			private set;
		}

		private readonly DataAccessModelConfiguration configuration;

		public BaseTests(string providerName)
		{
			this.ProviderName = providerName;

			XmlConfigurator.Configure();

			try
			{
				configuration = this.Create(providerName, this.GetType().Name);
				model = DataAccessModel.BuildDataAccessModel<TestDataAccessModel>(configuration);
				model.Create(DatabaseCreationOptions.DeleteExisting);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);

				throw;
			}
		}

		[TestFixtureTearDown]
		public virtual void Dispose()
		{
			model.Dispose();
		}
	}
}
