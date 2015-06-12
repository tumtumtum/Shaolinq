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
			var deleteDatabaseDropsTablesOnly = ((SqlServerSqlDatabaseContext)this.SqlDatabaseContext).DeleteDatabaseDropsTablesOnly;

			using (var connection = factory.CreateConnection())
			{
				connection.ConnectionString = deleteDatabaseDropsTablesOnly ? this.SqlDatabaseContext.ConnectionString : this.SqlDatabaseContext.ServerConnectionString;

				connection.Open();

				if (deleteDatabaseDropsTablesOnly)
				{
					if (overwrite)
					{
						using (var command = (SqlCommand) connection.CreateCommand())
						{
							command.CommandTimeout = Math.Min((int)this.SqlDatabaseContext.CommandTimeout.TotalSeconds, 300);
							command.CommandText = @"
while(exists(select 1 from INFORMATION_SCHEMA.TABLE_CONSTRAINTS where CONSTRAINT_TYPE='FOREIGN KEY'))
begin
 declare @sql nvarchar(2000)
 SELECT TOP 1 @sql=('ALTER TABLE ' + TABLE_SCHEMA + '.[' + TABLE_NAME
 + '] DROP CONSTRAINT [' + CONSTRAINT_NAME + ']')
 FROM information_schema.table_constraints
 WHERE CONSTRAINT_TYPE = 'FOREIGN KEY'
 exec (@sql)
 PRINT @sql
end
								";
							command.ExecuteNonQuery();
						}

						using (var command = (SqlCommand)connection.CreateCommand())
						{
							command.CommandTimeout = Math.Min((int)this.SqlDatabaseContext.CommandTimeout.TotalSeconds, 300);
							command.CommandText = @"
while(exists(select 1 from INFORMATION_SCHEMA.TABLES))
begin
 declare @sql nvarchar(2000)
 SELECT TOP 1 @sql=('DROP TABLE ' + TABLE_SCHEMA + '.[' + TABLE_NAME
 + ']')
 FROM INFORMATION_SCHEMA.TABLES
 WHERE TABLE_NAME != '__MigrationHistory'
exec (@sql)
 PRINT @sql
end
								";
							command.ExecuteNonQuery();
						}
					}

					return true;
				}

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
