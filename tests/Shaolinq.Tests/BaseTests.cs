// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using Shaolinq.MySql;
﻿using Shaolinq.Postgres.Devart;
﻿using Shaolinq.Sqlite;
using Shaolinq.Tests.DataAccessModel.Test;
using log4net.Config;

namespace Shaolinq.Tests
{
	public class BaseTests
	{
		protected TestDataAccessModel model;

		public void Foo()
		{
		}

		protected DataAccessModelConfiguration CreateMySqlConfiguration(string contextName, string databaseName)
		{
			return new DataAccessModelConfiguration()
			{
				PersistenceContexts = new PersistenceContextInfo[]
				{
					new MySqlPersistenceContextInfo()
					{
						ContextName = contextName,
						DatabaseName = databaseName,
						DatabaseConnectionInfos = new MySqlDatabaseConnectionInfo[]
						{
							new MySqlDatabaseConnectionInfo()
							{
								PersistenceMode = PersistenceMode.ReadWrite,
								ServerName = "localhost",
								UserName = "root",
								Password = "root"
							}
						}
					}
				}
			};
		}

		protected DataAccessModelConfiguration CreateSqliteConfiguration(string contextName, string databaseName)
		{
			return new DataAccessModelConfiguration()
			{
				PersistenceContexts = new PersistenceContextInfo[]
				{
					new SqlitePersistenceContextInfo()
					{
						ContextName = contextName,
						DatabaseName = databaseName,
						DatabaseConnectionInfos = new SqliteDatabaseConnectionInfo[]
						{
							new SqliteDatabaseConnectionInfo()
							{
								PersistenceMode = PersistenceMode.ReadWrite,
								FileName = databaseName + ".db"
							}
						}
					}
				}
			};
		}

		protected DataAccessModelConfiguration CreatePostgresDevartConfiguration(string contextName, string databaseName)
		{
			return new DataAccessModelConfiguration()
			{
				PersistenceContexts = new PersistenceContextInfo[]
				{
					new PostgresDevartPersistenceContextInfo()
					{
						ContextName = contextName,
						DatabaseName = databaseName,
						DatabaseConnectionInfos = new PostgresDevartDatabaseConnectionInfo[]
						{
							new PostgresDevartDatabaseConnectionInfo()
							{
								ServerName = "localhost",
								UserId = "postgres",
								Password = "postgres",
								PersistenceMode = PersistenceMode.ReadWrite
							}
						}
					}
				}
			};
		}

		protected DataAccessModelConfiguration CreateConfiguration(string providerName, string contextName, string databaseName)
		{
			if (providerName == "Postgres.Devart")
			{
				return this.CreatePostgresDevartConfiguration(contextName, databaseName);
			}
			else if (providerName == "MySql")
			{
				return this.CreateMySqlConfiguration(contextName, databaseName);
			}
			else
			{
				return this.CreateSqliteConfiguration(contextName, databaseName);
			}
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
				model = BaseDataAccessModel.BuildDataAccessModel<TestDataAccessModel>(configuration);
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
