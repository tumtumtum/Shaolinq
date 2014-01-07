// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Transactions;
using MySql.Data.MySqlClient;
﻿using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public class MySqlSqlDatabaseTransactionContext
		: SqlDatabaseTransactionContext
	{
		protected override char ParameterIndicatorChar
		{
			get
			{
				return '?';
			}
		}

		public MySqlSqlDatabaseTransactionContext(SystemDataBasedSqlDatabaseContext sqlDatabaseContext, DataAccessModel dataAccessModel, Transaction transaction)
			: base(sqlDatabaseContext, dataAccessModel, transaction)
		{
		}
	}
}
