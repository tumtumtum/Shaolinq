// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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
				connection.ConnectionString = deleteDatabaseDropsTablesOnly ? this.SqlDatabaseContext.ConnectionString : this.SqlDatabaseContext.ServerConnectionString;

				connection.Open();

				if (deleteDatabaseDropsTablesOnly)
				{
					using (var command = (SqlCommand) connection.CreateCommand())
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
					}

					using (var command = (SqlCommand)connection.CreateCommand())
					{
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
					
					return true;
				}

				using (var command = (SqlCommand)connection.CreateCommand())
				{
					try
					{
						var databaseName = this.SqlDatabaseContext.DatabaseName.Trim();

						if (options == DatabaseCreationOptions.DeleteExistingDatabase)
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
