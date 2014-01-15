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
			return PostgresConfiguration.Create(databaseName, "localhost", "postgres", "postgres");
		}

		protected DataAccessModelConfiguration CreatePostgresDotConnectConfiguration(string databaseName)
		{
			return PostgresDotConnectConfiguration.Create("DotConnect" + databaseName, "localhost", "postgres", "postgres");
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
				Console.WriteLine(e.StackTrace);

				throw;
			}
		}

		[TearDown]
		public virtual void TearDown()
		{
			model.Dispose();
		}
	}
}
