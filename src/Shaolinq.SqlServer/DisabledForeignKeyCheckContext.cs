// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	internal class DisabledForeignKeyCheckContext
		: IDisabledForeignKeyCheckContext
	{
		private readonly SqlTransactionalCommandsContext context;

		public DisabledForeignKeyCheckContext(SqlTransactionalCommandsContext context)
		{
			this.context = context;

			var command = ((DefaultSqlTransactionalCommandsContext)context).CreateCommand();

			// LogicalNot Azure compatible - command.CommandText = "EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"";

			command.CommandText = 
@"DECLARE @table_name SYSNAME;
DECLARE @schema_name SYSNAME;
DECLARE @cmd NVARCHAR(MAX);
DECLARE table_cursor CURSOR FOR SELECT t.name, s.name FROM sys.tables t INNER JOIN sys.schemas s ON s.schema_id = t.schema_id;

OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @table_name, @schema_name;

WHILE @@FETCH_STATUS = 0 BEGIN
  SELECT @cmd = 'ALTER TABLE '+QUOTENAME(@schema_name)+'.'+QUOTENAME(@table_name)+' NOCHECK CONSTRAINT ALL';
  EXEC (@cmd);
  FETCH NEXT FROM table_cursor INTO @table_name, @schema_name;
END

CLOSE table_cursor;
DEALLOCATE table_cursor;";

			command.ExecuteNonQuery();
		}

		public virtual void Dispose()
		{
			var command = ((DefaultSqlTransactionalCommandsContext)this.context).CreateCommand();

			// LogicalNot Azure compatible - command.CommandText = "exec sp_msforeachtable @command1=\"print '?'\", @command2=\"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\";";

			command.CommandText =
@"DECLARE @table_name SYSNAME;
DECLARE @schema_name SYSNAME;
DECLARE @cmd NVARCHAR(MAX);
DECLARE table_cursor CURSOR FOR SELECT t.name, s.name FROM sys.tables t INNER JOIN sys.schemas s ON s.schema_id = t.schema_id;

OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @table_name, @schema_name;

WHILE @@FETCH_STATUS = 0 BEGIN
  SELECT @cmd = 'ALTER TABLE '+QUOTENAME(@schema_name)+'.'+QUOTENAME(@table_name)+' WITH CHECK CHECK CONSTRAINT ALL';
  EXEC (@cmd);
  FETCH NEXT FROM table_cursor INTO @table_name, @schema_name;
END

CLOSE table_cursor;
DEALLOCATE table_cursor;";

			command.ExecuteNonQuery();
		}
	}
}
