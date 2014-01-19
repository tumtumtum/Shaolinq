// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedSqlDatabaseSchemaManager
		: SqlDatabaseSchemaManager
	{
		public PostgresSharedSqlDatabaseSchemaManager(SqlDatabaseContext sqlDatabaseContext)
			: base(sqlDatabaseContext)
		{
		}

		protected override SqlDataDefinitionBuilderFlags GetBuilderFlags()
		{
			var retval = base.GetBuilderFlags();

			if (((PostgresSharedSqlDataTypeProvider)this.sqlDatabaseContext.SqlDataTypeProvider).NativeEnums)
			{
				retval |= SqlDataDefinitionBuilderFlags.BuildEnums;
			}

			return retval;
		}

		protected override bool CreateDatabaseOnly(bool overwrite)
		{
			var retval = false;
			var factory = this.sqlDatabaseContext.CreateDbProviderFactory();
			var databaseName = this.sqlDatabaseContext.DatabaseName;

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

								if (s.Equals(databaseName))
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
							command.CommandText = String.Concat("DROP DATABASE \"", databaseName, "\";");
							command.ExecuteNonQuery();
						}
					}

					using (command = dbConnection.CreateCommand())
					{
						command.CommandText = String.Concat("CREATE DATABASE \"", databaseName, "\" WITH ENCODING 'UTF8';");
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
							command.CommandText = String.Concat("CREATE DATABASE \"", databaseName, "\" WITH ENCODING 'UTF8';");
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
