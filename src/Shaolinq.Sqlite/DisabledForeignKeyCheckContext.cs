// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	internal class DisabledForeignKeyCheckContext
		: IDisabledForeignKeyCheckContext
	{
		private readonly SqlTransactionalCommandsContext context;

		public DisabledForeignKeyCheckContext(SqlTransactionalCommandsContext context)
		{
			this.context = context;

			using (var command = ((DefaultSqlTransactionalCommandsContext)context).CreateCommand())
			{
				command.CommandText = "PRAGMA foriegn_keys = OFF;";

				command.ExecuteNonQuery();
			}
		}

		public virtual void Dispose()
		{
			using (var command = ((DefaultSqlTransactionalCommandsContext)context).CreateCommand())
			{
				command.CommandText = "PRAGMA foriegn_keys = ON;";

				command.ExecuteNonQuery();
			}
		}
	}
}
