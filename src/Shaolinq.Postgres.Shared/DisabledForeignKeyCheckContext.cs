// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class DisabledForeignKeyCheckContext
		: IDisabledForeignKeyCheckContext
	{
		public DisabledForeignKeyCheckContext(DatabaseTransactionContext context)
		{
			var command = ((SqlDatabaseTransactionContext)context).DbConnection.CreateCommand();

			command.CommandText = "SET CONSTRAINTS ALL DEFERRED;";
			command.ExecuteNonQuery();
		}

		public virtual void Dispose()
		{
		}
	}
}
