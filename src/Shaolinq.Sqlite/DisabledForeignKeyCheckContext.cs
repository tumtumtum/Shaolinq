// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.Sqlite
{
	internal class DisabledForeignKeyCheckContext
		: IDisabledForeignKeyCheckContext
	{
		public DisabledForeignKeyCheckContext(DatabaseTransactionContext context)
		{
		}

		public virtual void Dispose()
		{
		}
	}
}
