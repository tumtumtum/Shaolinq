// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Data.Common;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Sql;

namespace Shaolinq.Postgres.Shared
{
	public abstract class PostgresSharedDatabaseCreator
		: DatabaseCreator
	{
		protected readonly SystemDataBasedDatabaseConnection  connection;

		protected PostgresSharedDatabaseCreator(SystemDataBasedDatabaseConnection connection, DataAccessModel model)
			: base(model)
		{
			this.connection = connection;
		}

		#region CreateDatabaseOnly

		protected abstract string GetDatabaselessConnectionString();
		protected abstract DbProviderFactory CreateDbProviderFactory();

		protected override bool CreateDatabaseOnly(bool overwrite)
		{
			var retval = false;
			var factory = this.CreateDbProviderFactory();

			this.connection.DropAllConnections();

			using (var dbConnection = factory.CreateConnection())
			{
				dbConnection.ConnectionString = this.GetDatabaselessConnectionString();
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

								if (s.Equals(this.connection.DatabaseName))
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
							command.CommandText = String.Concat("DROP DATABASE \"", this.connection.DatabaseName, "\";");
							command.ExecuteNonQuery();
						}
					}

					using (command = dbConnection.CreateCommand())
					{
						command.CommandText = String.Concat("CREATE DATABASE \"", this.connection.DatabaseName, "\" WITH ENCODING 'UTF8';");
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
							command.CommandText = String.Concat("CREATE DATABASE \"", this.connection.DatabaseName, "\" WITH ENCODING 'UTF8';");
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

		#endregion
	}
}
