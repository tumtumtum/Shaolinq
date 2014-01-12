// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Transactions;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDatabaseTransactionContext
		: DefaultSqlDatabaseTransactionContext
	{
		public SqliteSqlDatabaseTransactionContext(SqlDatabaseContext sqlDatabaseContext, Transaction transaction)
			: base(sqlDatabaseContext, transaction)
		{
		}

		public virtual void RealDispose()
		{
			base.Dispose();
		}

		public override void Dispose()
		{
			if (!String.Equals(((SqliteSqlDatabaseContext)this.SqlDatabaseContext).FileName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				base.Dispose();
			}
		}
	}
}
