// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedDatabaseCreator
		: DatabaseCreator
	{
		public string DatabaseName { get; private set; }
		protected readonly SqlDatabaseContext sqlDatabaseContext;

		public PostgresSharedDatabaseCreator(SqlDatabaseContext sqlDatabaseContext, DataAccessModel model, string databaseName)
			: base(model)
		{
			this.DatabaseName = databaseName;
			this.sqlDatabaseContext = sqlDatabaseContext;
		}

		protected override bool CreateDatabaseOnly(bool overwrite)
		{
			var retval = false;
			var factory = this.sqlDatabaseContext.CreateDbProviderFactory();

			this.sqlDatabaseContext.DropAllConnections();

			using (var dbConnection = factory.CreateConnection())
			{
				dbConnection.ConnectionString = this.sqlDatabaseContext.ServerConnectionString;
				dbConnection.Open();

				IDbCommand command;

				if (overwrite)
				{
					bool drop = false;

					using (command = dbConnection.CreateCommand())
					{
						command.CommandText = String.Format("SELECT datname FROM pg_database;");

						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								string s = reader.GetString(0);

								if (s.Equals(this.DatabaseName))
								{
									drop = true;

									break;
								}
							}
						}
					}

					if (drop)
					{
						using (command = dbConnection.CreateCommand())
						{
							command.CommandText = String.Concat("DROP DATABASE \"", this.DatabaseName, "\";");
							command.ExecuteNonQuery();
						}
					}

					using (command = dbConnection.CreateCommand())
					{
						command.CommandText = String.Concat("CREATE DATABASE \"", this.DatabaseName, "\" WITH ENCODING 'UTF8';");
						command.ExecuteNonQuery();
					}

					retval = true;
				}
				else
				{
					try
					{
						using (command = dbConnection.CreateCommand())
						{
							command.CommandText = String.Concat("CREATE DATABASE \"", this.DatabaseName, "\" WITH ENCODING 'UTF8';");
							command.ExecuteNonQuery();
						}

						retval = true;
					}
					catch
					{
						retval = false;
					}
				}
			}

			return retval;
		}
	}
}
