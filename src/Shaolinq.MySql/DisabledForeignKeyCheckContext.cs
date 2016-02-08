// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	internal class DisabledForeignKeyCheckContext
		: IDisabledForeignKeyCheckContext
	{
		private readonly SqlTransactionalCommandsContext context;

		public DisabledForeignKeyCheckContext(SqlTransactionalCommandsContext context)
		{
			this.context = context;

			var command = ((DefaultSqlTransactionalCommandsContext)context).CreateCommand();

			command.CommandText = "SET FOREIGN_KEY_CHECKS = 0;";

			command.ExecuteNonQuery();
		}

		public virtual void Dispose()
		{
			var command = ((DefaultSqlTransactionalCommandsContext) this.context).CreateCommand();

			command.CommandText = "SET FOREIGN_KEY_CHECKS = 1;";

			command.ExecuteNonQuery();
		}
	}
}
