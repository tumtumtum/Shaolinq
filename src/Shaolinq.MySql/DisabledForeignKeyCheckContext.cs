// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	internal class DisabledForeignKeyCheckContext
		: IDisabledForeignKeyCheckContext
	{
		private readonly DatabaseTransactionContext context;

		public DisabledForeignKeyCheckContext(DatabaseTransactionContext context)
		{
			this.context = context;

			var command = ((SqlDatabaseTransactionContext)context).DbConnection.CreateCommand();

			command.CommandText = "SET FOREIGN_KEY_CHECKS = 0;";

			command.ExecuteNonQuery();
		}

		public virtual void Dispose()
		{
			var command = ((SqlDatabaseTransactionContext)context).DbConnection.CreateCommand();

			command.CommandText = "SET FOREIGN_KEY_CHECKS = 1;";

			command.ExecuteNonQuery();
		}
	}
}
