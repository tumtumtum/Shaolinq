// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public partial class MySqlSqlDatabaseSchemaManager
		: SqlDatabaseSchemaManager
	{
		public MySqlSqlDatabaseSchemaManager(MySqlSqlDatabaseContext sqlDatabaseContext)
			: base(sqlDatabaseContext)
		{
		}

        [RewriteAsync]
        protected override bool CreateDatabaseOnly(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			var retval = false;
			var factory = this.SqlDatabaseContext.CreateDbProviderFactory();
			var overwrite = options == DatabaseCreationOptions.DeleteExistingDatabase;

			using (var dbConnection = factory.CreateConnection())
			{
				dbConnection.ConnectionString = this.SqlDatabaseContext.ServerConnectionString;

				dbConnection.Open();

				using (var command = dbConnection.CreateCommand())
				{
					if (overwrite)
					{
						var drop = false;

						command.CommandText = String.Format("SHOW DATABASES;", this.SqlDatabaseContext.DatabaseName);

						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								var s = reader.GetString(0);

								if (s.Equals(this.SqlDatabaseContext.DatabaseName) ||
									s.Equals(this.SqlDatabaseContext.DatabaseName.ToLower()))
								{
									drop = true;

									break;
								}
							}
						}

						if (drop)
						{
							command.CommandText = $"DROP DATABASE {this.SqlDatabaseContext.DatabaseName}";
                            command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
                        }

						command.CommandText = $"CREATE DATABASE {this.SqlDatabaseContext.DatabaseName}\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;";
                        command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);

                        retval = true;
					}
					else
					{
						try
						{
							command.CommandText = $"CREATE DATABASE {this.SqlDatabaseContext.DatabaseName}\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;";
                            command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);

                            retval = true;
						}
						catch
						{
							retval = false;
						}
					}
				}
			}

			return retval;
		}
	}
}
