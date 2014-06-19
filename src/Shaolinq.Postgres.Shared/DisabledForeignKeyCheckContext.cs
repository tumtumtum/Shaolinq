// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class DisabledForeignKeyCheckContext
		: IDisabledForeignKeyCheckContext
	{
		public DisabledForeignKeyCheckContext(SqlTransactionalCommandsContext context)
		{
			var command = ((DefaultSqlTransactionalCommandsContext)context).DbConnection.CreateCommand();

			command.CommandText = "SET CONSTRAINTS ALL DEFERRED;";
			command.ExecuteNonQuery();
		}

		public virtual void Dispose()
		{
		}
	}
}
