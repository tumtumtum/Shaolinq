// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;
using System.Transactions;
using log4net.Config;
using NUnit.Framework;
using Shaolinq.MySql;
using Shaolinq.Postgres;
using Shaolinq.Postgres.DotConnect;
using Shaolinq.Sqlite;
using Shaolinq.SqlServer;

namespace Shaolinq.Tests
{
	public class BaseTests<T>
		where T : DataAccessModel
	{
		protected T model;
		protected internal static readonly bool useMonoData;
		private readonly bool useDataAccessScope;

		static BaseTests()
		{
			useMonoData = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SHAOLINQ_TESTS_USING_MONODATA"));
		}
		
		protected TransactionScopeAdapter NewTransactionScope()
		{
			if (this.useDataAccessScope)
			{
				return new TransactionScopeAdapter(new DataAccessScope());
			}
			else
			{
				return new TransactionScopeAdapter(new TransactionScope());
			}
		}

		protected DataAccessModelConfiguration CreateSqlServerConfiguration(string databaseName)
		{
		    var host = Environment.GetEnvironmentVariable("SHAOLINQ_TESTS_SQLSERVER") ?? ".\\SQLEXPRESS";

            var retval = SqlServerConfiguration.Create(databaseName, host, multipleActiveResultsets: true);

			retval.SaveAndReuseGeneratedAssemblies = true;

		    return retval;
		}

		protected DataAccessModelConfiguration CreateMySqlConfiguration(string databaseName)
		{
			var retval = MySqlConfiguration.Create(databaseName, "localhost", "root", "root");

			retval.SaveAndReuseGeneratedAssemblies = true;

			return retval;
		}

		protected DataAccessModelConfiguration CreateSqliteConfiguration(string databaseName)
		{
			var retval = SqliteConfiguration.Create(databaseName + ".db", null, useMonoData);

			retval.SaveAndReuseGeneratedAssemblies = true;

			return retval;
		}

		protected DataAccessModelConfiguration CreateSqliteInMemoryConfiguration(string databaseName)
		{
			var retval = SqliteConfiguration.Create("file:" + databaseName + "?mode=memory&cache=shared", null, useMonoData);

			retval.SaveAndReuseGeneratedAssemblies = true;

			return retval;
		}

		protected DataAccessModelConfiguration CreateSqliteClassicInMemoryConfiguration(string databaseName)
		{
			var retval = SqliteConfiguration.Create(":memory:", null, useMonoData);

			retval.SaveAndReuseGeneratedAssemblies = true;

			return retval;
		}

		protected DataAccessModelConfiguration CreatePostgresConfiguration(string databaseName)
		{
			var retval = PostgresConfiguration.Create(new PostgresSqlDatabaseContextInfo()
			{
				DatabaseName = databaseName,	
				ServerName = "localhost",
				UserId = "postgres",
				Password = "postgres",
				NativeEnums = false,
				Categories = null,
				Pooling = true,
				MinPoolSize = 10,
				MaxPoolSize = 10
			});

			retval.SaveAndReuseGeneratedAssemblies = true;

			return retval;
		}

		protected DataAccessModelConfiguration CreatePostgresDotConnectConfiguration(string databaseName)
		{
			var retval = PostgresDotConnectConfiguration.Create(new PostgresDotConnectSqlDatabaseContextInfo
			{
				DatabaseName = "DotConnectPrepared" + databaseName,
				ServerName = "localhost",
				UserId = "postgres",
				Password = "postgres",
				Categories = null,
				UnpreparedExecute = false
			});

			retval.SaveAndReuseGeneratedAssemblies = true;

			return retval;
		}

		protected DataAccessModelConfiguration CreatePostgresDotConnectUnpreparedConfiguration(string databaseName)
		{
			var retval = PostgresDotConnectConfiguration.Create(new PostgresDotConnectSqlDatabaseContextInfo
			{
				DatabaseName = "DotConnectUnprepared" + databaseName,
				ServerName = "localhost",
				UserId = "postgres",
				Password = "postgres",
				Categories = null,
				UnpreparedExecute = false
			});

			retval.SaveAndReuseGeneratedAssemblies = true;

			return retval;
		}

		protected DataAccessModelConfiguration Create(string providerName, string databaseName)
		{
			var methodInfo = this.GetType().GetMethod("Create" + providerName.Replace(".", "") + "Configuration", BindingFlags.Instance | BindingFlags.NonPublic);

			return (DataAccessModelConfiguration)methodInfo.Invoke(this, new object[] { databaseName });
		}

		protected string ProviderName { get; }

		public BaseTests(string providerName)
		{
			var ss = providerName.Split(':');

			if (ss.Length <= 1)
			{
				this.ProviderName = providerName;
			}
			else
			{
				this.ProviderName = ss[0];
				this.useDataAccessScope = ss[1] == "DataAccessScope";
			}

			XmlConfigurator.Configure();

			try
			{
				if (providerName == "default")
				{
					this.model = DataAccessModel.BuildDataAccessModel<T>();
				}
				else
				{
					var configuration = this.Create(this.ProviderName, this.GetType().Name);
					this.model = DataAccessModel.BuildDataAccessModel<T>(configuration);
				}

				this.model.Create(DatabaseCreationOptions.DeleteExistingDatabase);
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception while configuring provider: " + providerName);
				Console.WriteLine(e);
				Console.WriteLine(e.StackTrace);

				throw;
			}
		}

		[OneTimeTearDown]
		public virtual void Dispose()
		{
			this.model.Dispose();
		}
	}
}
