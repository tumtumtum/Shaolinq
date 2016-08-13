namespace Shaolinq.Postgres
{
#pragma warning disable
	using System;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using Shaolinq;
	using Shaolinq.Postgres;
	using Shaolinq.Persistence;
	using Shaolinq.Persistence.Linq;

	internal partial class PostgresSqlDatabaseSchemaManager
	{
		protected override Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			return CreateDatabaseOnlyAsync(dataDefinitionExpressions, options, CancellationToken.None);
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
							while (await reader.ReadExAsync(cancellationToken).ConfigureAwait(false))
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
	}
}

namespace Shaolinq.Postgres
{
#pragma warning disable
	using System;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using Npgsql;
	using Platform;
	using Shaolinq;
	using NpgsqlTypes;
	using Shaolinq.Postgres;
	using Shaolinq.Persistence;

	public partial class PostgresSqlTransactionalCommandsContext
	{
		public override Task CommitAsync()
		{
			return CommitAsync(CancellationToken.None);
		}

		public override async Task CommitAsync(CancellationToken cancellationToken)
		{
			if (this.preparedTransactionName != null)
			{
				using (var command = this.CreateCommand())
				{
					command.CommandText = $"COMMIT PREPARED '{this.preparedTransactionName}';";
					await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken).ConfigureAwait(false);
				}
			}

			await base.CommitAsync(cancellationToken).ConfigureAwait(false);
		}

		public override Task RollbackAsync()
		{
			return RollbackAsync(CancellationToken.None);
		}

		public override async Task RollbackAsync(CancellationToken cancellationToken)
		{
			if (this.preparedTransactionName != null)
			{
				using (var command = this.CreateCommand())
				{
					command.CommandText = $"ROLLBACK PREPARED '{this.preparedTransactionName}';";
					await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken).ConfigureAwait(false);
				}
			}

			await base.RollbackAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}