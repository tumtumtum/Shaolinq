using System;
using System.Data.SqlClient;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlDatabaseSchemaManager
		: SqlDatabaseSchemaManager
	{
		public SqlServerSqlDatabaseSchemaManager(SqlServerSqlDatabaseContext sqlDatabaseContext)
			: base(sqlDatabaseContext)
		{
		}

		protected override bool CreateDatabaseOnly(bool overwrite)
		{
			var factory = this.SqlDatabaseContext.CreateDbProviderFactory();

			using (var connection = factory.CreateConnection())
			{
				connection.ConnectionString = this.SqlDatabaseContext.ServerConnectionString;

				connection.Open();

				using (var command = (SqlCommand)connection.CreateCommand())
				{
					var databaseName = this.SqlDatabaseContext.DatabaseName.Trim();

					try
					{
						if (overwrite)
						{
							command.CommandText = string.Format("IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = '{0}') DROP DATABASE [{0}];", databaseName);
							command.ExecuteNonQuery();
						}

						command.CommandText = string.Format("CREATE DATABASE [{0}];", databaseName);
						command.ExecuteNonQuery();

						return true;
					}
					catch
					{
						return false;
					}
				}
			}
		}
	}
}
