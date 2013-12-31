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

		protected override bool IsDataAccessException(Exception e)
		{
			return e is MySqlException;
		}

		protected override bool IsConcurrencyException(Exception e)
		{
			return false;
		}

		public MySqlSqlDatabaseTransactionContext(SystemDataBasedSqlDatabaseContext sqlDatabaseContext, DataAccessModel dataAccessModel, Transaction transaction)
			: base(sqlDatabaseContext, dataAccessModel, transaction)
		{
		}
	}
}
