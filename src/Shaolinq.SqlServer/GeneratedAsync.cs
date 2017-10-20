namespace Shaolinq.SqlServer
{
#pragma warning disable
	using System;
	using System.Threading;
	using System.Data.SqlClient;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using Shaolinq;
	using Shaolinq.SqlServer;
	using Shaolinq.Persistence;

	public partial class SqlServerSqlDatabaseSchemaManager
	{
		protected virtual Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			return this.CreateDatabaseOnlyAsync(dataDefinitionExpressions, options, CancellationToken.None);
		}

		protected async virtual Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options, CancellationToken cancellationToken)
		{
			var factory = this.SqlDatabaseContext.CreateDbProviderFactory();
			var deleteDatabaseDropsTablesOnly = ((SqlServerSqlDatabaseContext)this.SqlDatabaseContext).DeleteDatabaseDropsTablesOnly;
			using (var connection = factory.CreateConnection())
			{
				if (connection == null)
				{
					throw new InvalidOperationException($"Unable to create connection from {factory}");
				}

				try
				{
					var databaseName = this.SqlDatabaseContext.DatabaseName.Trim();
					var context = (SqlServerSqlDatabaseContext)this.SqlDatabaseContext;
					connection.ConnectionString = deleteDatabaseDropsTablesOnly ? this.SqlDatabaseContext.ConnectionString : this.SqlDatabaseContext.ServerConnectionString;
					connection.Open();
					using (var command = (SqlCommand)connection.CreateCommand())
					{
						if (options == DatabaseCreationOptions.DeleteExistingDatabase)
						{
							if (deleteDatabaseDropsTablesOnly)
							{
								command.CommandTimeout = Math.Min((int)(this.SqlDatabaseContext.CommandTimeout?.TotalSeconds ?? SqlDatabaseContextInfo.DefaultCommandTimeout), 300);
								command.CommandText = @"
									WHILE(exists(select 1 from INFORMATION_SCHEMA.TABLE_CONSTRAINTS where CONSTRAINT_TYPE='FOREIGN KEY'))
									BEGIN
										DECLARE @sql nvarchar(2000)
										SELECT TOP 1 @sql=('ALTER TABLE ' + TABLE_SCHEMA + '.[' + TABLE_NAME + '] DROP CONSTRAINT [' + CONSTRAINT_NAME + ']')
										FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_TYPE = 'FOREIGN KEY'
										EXEC (@sql)
									END
								";
								command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
								command.CommandText = @"
									WHILE(exists(select 1 from INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA != 'sys' AND TABLE_TYPE = 'BASE TABLE'))
									BEGIN
										declare @sql nvarchar(2000)
										SELECT TOP 1 @sql=('DROP TABLE ' + TABLE_SCHEMA + '.[' + TABLE_NAME + ']')
										FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA != 'sys' AND TABLE_TYPE = 'BASE TABLE'
										EXEC (@sql)
									END
								";
								command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
								command.CommandText = $"IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE NAME = '{databaseName}') CREATE DATABASE [{databaseName}];";
								command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
							}
							else
							{
								command.CommandText = $"IF EXISTS (SELECT 1 FROM sys.databases WHERE NAME = '{databaseName}') DROP DATABASE [{databaseName}];";
								command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
								command.CommandText = $"CREATE DATABASE [{databaseName}];";
								command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
							}
						}
						else
						{
							command.CommandText = $"IF EXISTS (SELECT 1 FROM sys.databases WHERE NAME = '{databaseName}') DROP DATABASE [{databaseName}];";
							command.CommandText = $"CREATE DATABASE [{databaseName}];";
							command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
						}

						command.CommandText = $"ALTER DATABASE [{databaseName}] SET ALLOW_SNAPSHOT_ISOLATION {(context.AllowSnapshotIsolation ? "ON" : "OFF")};";
						command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
						command.CommandText = $"ALTER DATABASE [{databaseName}] SET READ_COMMITTED_SNAPSHOT {(context.ReadCommittedSnapshot ? "ON" : "OFF")};";
						command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
						return true;
					}
				}
				catch (Exception e)
				{
					Logger.Log(Logging.LogLevel.Error, () => "Exception creating database: " + e);
					throw;
				}
			}
		}
	}
}