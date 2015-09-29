// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
﻿using System.Reflection;
using NUnit.Framework;
using Shaolinq.MySql;
﻿using Shaolinq.Postgres;
﻿using Shaolinq.Postgres.DotConnect;
﻿using Shaolinq.Sqlite;
using log4net.Config;
using Platform;
using Platform.Reflection;
using Shaolinq.SqlServer;

namespace Shaolinq.Tests
{
    public class BaseTests<T>
		where T : DataAccessModel
    {
        protected T model;
	    protected internal static readonly bool useMonoData;

	    static BaseTests()
	    {
		    useMonoData = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SHAOLINQ_TESTS_USING_MONODATA"));
	    }

		protected DataAccessModelConfiguration CreateSqlServerConfiguration(string databaseName)
		{
			return SqlServerConfiguration.Create(databaseName, "localhost", null, null);
		}

	    protected DataAccessModelConfiguration CreateMySqlConfiguration(string databaseName)
        {
            return MySqlConfiguration.Create(databaseName, "localhost", "root", "root");
        }

        protected DataAccessModelConfiguration CreateSqliteConfiguration(string databaseName)
        {
            return SqliteConfiguration.Create(databaseName + ".db", null, useMonoData);
        }

		protected DataAccessModelConfiguration CreateSqliteInMemoryConfiguration(string databaseName)
        {
            return SqliteConfiguration.Create("file:" + databaseName + "?mode=memory&cache=shared", null, useMonoData);
        }

        protected DataAccessModelConfiguration CreateSqliteClassicInMemoryConfiguration(string databaseName)
        {
            return SqliteConfiguration.Create(":memory:", null, useMonoData);
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
	        return PostgresDotConnectConfiguration.Create(new PostgresDotConnectSqlDatabaseContextInfo
            {
                DatabaseName = "DotConnectPrepared" + databaseName,
                ServerName = "localhost",
                UserId = "postgres",
                Password = "postgres",
                Categories = null,
                UnpreparedExecute = false
            });
        }

        protected DataAccessModelConfiguration CreatePostgresDotConnectUnpreparedConfiguration(string databaseName)
        {
            return PostgresDotConnectConfiguration.Create(new PostgresDotConnectSqlDatabaseContextInfo
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
                if (providerName == "default")
                {
                    model = DataAccessModel.BuildDataAccessModel<T>();
                }
                else
                {
                    configuration = this.Create(providerName, this.GetType().Name);
                    model = DataAccessModel.BuildDataAccessModel<T>(configuration);
                }

                model.Create(DatabaseCreationOptions.DeleteExistingDatabase);
            }
            catch (Exception e)
            {
	            Console.WriteLine("Exception while configuring provider: " + providerName);
                Console.WriteLine(e);
				Console.WriteLine(e.StackTrace);

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
