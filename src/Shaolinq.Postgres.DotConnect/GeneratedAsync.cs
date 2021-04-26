namespace Shaolinq.Postgres
{
#pragma warning disable
	using System;
	// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
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
		protected override Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			return this.CreateDatabaseOnlyAsync(dataDefinitionExpressions, options, CancellationToken.None);
		}

		protected override async Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options, CancellationToken cancellationToken)
		{
			var retval = false;
			var factory = this.SqlDatabaseContext.CreateDbProviderFactory();
			var databaseName = this.SqlDatabaseContext.DatabaseName;
			var overwrite = options == DatabaseCreationOptions.DeleteExistingDatabase;
			this.SqlDatabaseContext.DropAllConnections();
			using (var dbConnection = factory.CreateConnection())
			{
				dbConnection.ConnectionString = this.SqlDatabaseContext.ServerConnectionString;
				await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
				IDbCommand command;
				if (overwrite)
				{
					var drop = false;
					using (command = dbConnection.CreateCommand())
					{
						command.CommandText = "SELECT datname FROM pg_database;";
						using (var reader = (await command.ExecuteReaderExAsync(this.SqlDatabaseContext.DataAccessModel, cancellationToken, true).ConfigureAwait(false)))
						{
							while ((await reader.ReadExAsync(cancellationToken).ConfigureAwait(false)))
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
							await command.ExecuteNonQueryExAsync(this.SqlDatabaseContext.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
						}
					}

					using (command = dbConnection.CreateCommand())
					{
						command.CommandText = $"CREATE DATABASE \"{databaseName}\" WITH ENCODING 'UTF8';";
						await command.ExecuteNonQueryExAsync(this.SqlDatabaseContext.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
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
							await command.ExecuteNonQueryExAsync(this.SqlDatabaseContext.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
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

		protected override Task CreateDatabaseSchemaAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			return this.CreateDatabaseSchemaAsync(dataDefinitionExpressions, options, CancellationToken.None);
		}

		protected override async Task CreateDatabaseSchemaAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options, CancellationToken cancellationToken)
		{
			if (!string.IsNullOrEmpty(this.SqlDatabaseContext.SchemaName))
			{
				var factory = this.SqlDatabaseContext.CreateDbProviderFactory();
				using (var dbConnection = factory.CreateConnection())
				{
					dbConnection.ConnectionString = this.SqlDatabaseContext.ServerConnectionString;
					await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
					dbConnection.ChangeDatabase(this.SqlDatabaseContext.DatabaseName);
					using (var command = dbConnection.CreateCommand())
					{
						command.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{this.SqlDatabaseContext.SchemaName}\";";
						await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
					}
				}
			}

			await base.CreateDatabaseSchemaAsync(dataDefinitionExpressions, options, cancellationToken).ConfigureAwait(false);
		}
	}
}