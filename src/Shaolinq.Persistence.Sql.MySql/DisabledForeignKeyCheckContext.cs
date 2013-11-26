// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Persistence.Sql.MySql
{
	internal class DisabledForeignKeyCheckContext
		: IDisabledForeignKeyCheckContext
	{
		private readonly PersistenceTransactionContext context;

		public DisabledForeignKeyCheckContext(PersistenceTransactionContext context)
		{
			this.context = context;

			var command = ((SqlPersistenceTransactionContext)context).DbConnection.CreateCommand();

			command.CommandText = "SET FOREIGN_KEY_CHECKS = 0;";

			command.ExecuteNonQuery();
		}

		public virtual void Dispose()
		{
			var command = ((SqlPersistenceTransactionContext)context).DbConnection.CreateCommand();

			command.CommandText = "SET FOREIGN_KEY_CHECKS = 1;";

			command.ExecuteNonQuery();
		}
	}
}
