// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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

			var command = ((DefaultSqlTransactionalCommandsContext)context).DbConnection.CreateCommand();

			command.CommandText = "EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"";

			command.ExecuteNonQuery();
		}

		public virtual void Dispose()
		{
			var command = ((DefaultSqlTransactionalCommandsContext)this.context).DbConnection.CreateCommand();

			command.CommandText = "exec sp_msforeachtable @command1=\"print '?'\", @command2=\"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\";";

			command.ExecuteNonQuery();
		}
	}
}
