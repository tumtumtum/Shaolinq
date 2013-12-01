// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
﻿using System.Reflection;
﻿using Shaolinq.MySql;
﻿using Shaolinq.Postgres;
﻿using Shaolinq.Postgres.DotConnect;
﻿using Shaolinq.Sqlite;
using Shaolinq.Tests.DataModels.Test;
using log4net.Config;

namespace Shaolinq.Tests
{
	public class BaseTests
	{
		protected TestDataAccessModel model;

		protected DataAccessModelConfiguration CreateMySqlConfiguration(string contextName, string databaseName)
		{
			return MySqlConfiguration.CreateConfiguration(contextName, databaseName, "localhost", "root", "root");
		}

		protected DataAccessModelConfiguration CreateSqliteConfiguration(string contextName, string databaseName)
		{
			return SqliteConfiguration.CreateConfiguration(contextName, databaseName + ".db");
		}

		protected DataAccessModelConfiguration CreatePostgresConfiguration(string contextName, string databaseName)
		{
			return PostgresConfiguration.CreateConfiguration(contextName, databaseName, "localhost", "postgres", "postgres");
		}

		protected DataAccessModelConfiguration CreatePostgresDotConnectConfiguration(string contextName, string databaseName)
		{
			return PostgresDotConnectConfiguration.CreateConfiguration(contextName, "DotConnect" + databaseName, "localhost", "postgres", "postgres");
		}

		protected DataAccessModelConfiguration CreateConfiguration(string providerName, string contextName, string databaseName)
		{
			var methodInfo = this.GetType().GetMethod("Create" + providerName.Replace(".", "") + "Configuration", BindingFlags.Instance | BindingFlags.NonPublic);

			return (DataAccessModelConfiguration)methodInfo.Invoke(this, new object[] {contextName, databaseName });
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
				configuration = this.CreateConfiguration(providerName, "Test", this.GetType().Name);
				model = DataAccessModel.BuildDataAccessModel<TestDataAccessModel>(configuration);
				model.CreateDatabases(true);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Console.WriteLine(e.StackTrace);

				throw;
			}
		}
	}
}
