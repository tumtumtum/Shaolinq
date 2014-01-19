﻿using System.Data;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqlitePersistentDbConnection
		: DbConnectionWrapper
	{
		public SqlitePersistentDbConnection(IDbConnection inner)
			: base(inner)
		{
		}

		public override void Dispose()
		{
		}

		public override void Close()
		{
		}
	}
}