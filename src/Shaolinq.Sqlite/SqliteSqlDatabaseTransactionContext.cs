// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Transactions;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDatabaseTransactionContext
		: SqlDatabaseTransactionContext
	{
		protected override char ParameterIndicatorChar
		{
			get
			{
				return '@';
			}
		}

		protected override bool IsDataAccessException(Exception e)
		{
			return e is SQLiteException;
		}
        
		protected override bool IsConcurrencyException(Exception e)
		{
			return false;
		}

		public SqliteSqlDatabaseTransactionContext(SystemDataBasedSqlDatabaseContext sqlDatabaseContext, DataAccessModel dataAccessModel, Transaction transaction)
			: base(sqlDatabaseContext, dataAccessModel, transaction)
		{
		}

		public virtual void RealDispose()
		{
			base.Dispose();
		}

		public override void Dispose()
		{
			if (!String.Equals(this.SqlDatabaseContext.DatabaseName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				base.Dispose();
			}
		}
	}
}
