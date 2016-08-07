// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.SqlClient;
using System.Linq.Expressions;
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

		protected override bool CreateDatabaseOnly(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
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
						if (deleteDatabaseDropsTablesOnly)
						{
							command.CommandTimeout = Math.Min((int)(this.SqlDatabaseContext.CommandTimeout?.TotalSeconds ?? SqlDatabaseContextInfo.DefaultCommandTimeout), 300);
							command.CommandText =
							@"
								WHILE(exists(select 1 from INFORMATION_SCHEMA.TABLE_CONSTRAINTS where CONSTRAINT_TYPE='FOREIGN KEY'))
								BEGIN
									DECLARE @sql nvarchar(2000)
									SELECT TOP 1 @sql=('ALTER TABLE ' + TABLE_SCHEMA + '.[' + TABLE_NAME + '] DROP CONSTRAINT [' + CONSTRAINT_NAME + ']')
									FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_TYPE = 'FOREIGN KEY'
									EXEC (@sql)
									PRINT @sql
								END
							";

							command.ExecuteNonQuery();

							command.CommandTimeout = Math.Min((int)(this.SqlDatabaseContext.CommandTimeout?.TotalSeconds ?? 300), 300);
							command.CommandText =
							@"
								WHILE(exists(select 1 from INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA != 'sys' AND TABLE_TYPE = 'BASE TABLE'))
								BEGIN
									declare @sql nvarchar(2000)
									SELECT TOP 1 @sql=('DROP TABLE ' + TABLE_SCHEMA + '.[' + TABLE_NAME + ']')
									FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA != 'sys' AND TABLE_TYPE = 'BASE TABLE'
									EXEC (@sql)
									PRINT @sql
								END
							";

							command.ExecuteNonQuery();
						}
						else
						{
							if (options == DatabaseCreationOptions.DeleteExistingDatabase)
							{
								command.CommandText = $"IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = '{databaseName}') DROP DATABASE [{databaseName}];";
								command.ExecuteNonQuery();
							}

							command.CommandText = $"CREATE DATABASE [{databaseName}];";
							command.ExecuteNonQuery();
						}

						command.CommandText = $"ALTER DATABASE [{databaseName}] SET ALLOW_SNAPSHOT_ISOLATION {(context.AllowSnapshotIsolation ? "ON" : "OFF")};";
						command.ExecuteNonQuery();

						command.CommandText = $"ALTER DATABASE [{databaseName}] SET READ_COMMITTED_SNAPSHOT {(context.ReadCommittedSnapshop ? "ON" : "OFF")};";
						command.ExecuteNonQuery();

						return true;
					}
				}
				catch
				{
					return false;
				}
			}
		}
	}
}
