using System;
using Shaolinq.Persistence.Sql.Sqlite;
using Shaolinq.Tests.DataAccessModel.Test;
using log4net.Config;

namespace Shaolinq.Tests
{
	public class BaseTests
	{
		protected TestDataAccessModel model;

		public void Foo()
		{
			short x = 10;

			Console.WriteLine(x);
		}

		public void Bar(bool v)
		{}

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

		protected DataAccessModelConfiguration CreateConfiguration(string providerName, string contextName, string databaseName)
		{
			return this.CreateSqliteConfiguration(contextName, databaseName);
		}
		
		private readonly string providerName;
		private readonly DataAccessModelConfiguration configuration;

		public BaseTests(string providerName)
		{
			XmlConfigurator.Configure();

			try
			{
				configuration = this.CreateConfiguration(providerName, "Test", this.GetType().Name);
				model = BaseDataAccessModel.BuildDataAccessModel<TestDataAccessModel>(configuration);
				model.CreateDatabases(true);

				this.providerName = providerName;
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
