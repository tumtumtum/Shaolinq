namespace Shaolinq.Postgres
{
#pragma warning disable
	using System;
	// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using Shaolinq;
	using Shaolinq.Postgres;
	using Shaolinq.Persistence;
	using Shaolinq.Persistence.Linq;

	public partial class PostgresSqlDatabaseSchemaManager
	{
		protected virtual Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			return this.CreateDatabaseOnlyAsync(dataDefinitionExpressions, options, CancellationToken.None);
		}

		protected async virtual Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options, CancellationToken cancellationToken)
		{
			var retval = false;
			var factory = this.SqlDatabaseContext.CreateDbProviderFactory();
			var databaseName = this.SqlDatabaseContext.DatabaseName;
			var overwrite = options == DatabaseCreationOptions.DeleteExistingDatabase;
			this.SqlDatabaseContext.DropAllConnections();
			using (var dbConnection = factory.CreateConnection())
			{
				dbConnection.ConnectionString = this.SqlDatabaseContext.ServerConnectionString;
				dbConnection.Open();
				IDbCommand command;
				if (overwrite)
				{
					var drop = false;
					using (command = dbConnection.CreateCommand())
					{
						command.CommandText = "SELECT datname FROM pg_database;";
						using (var reader = command.ExecuteReaderEx(this.SqlDatabaseContext.DataAccessModel, true))
						{
							while (reader.ReadEx())
							{
								var s = reader.GetString(0);
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
							command.CommandText = $"DROP DATABASE \"{databaseName}\";";
							command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
						}
					}

					using (command = dbConnection.CreateCommand())
					{
						command.CommandText = $"CREATE DATABASE \"{databaseName}\" WITH ENCODING 'UTF8';";
						command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
					}

					retval = true;
				}
				else
				{
					try
					{
						using (command = dbConnection.CreateCommand())
						{
							command.CommandText = $"CREATE DATABASE \"{databaseName}\" WITH ENCODING 'UTF8';";
							command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
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