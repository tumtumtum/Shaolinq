// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using System.Transactions;
using Devart.Data.PostgreSql;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectSqlTransactionalCommandsContext
		: DefaultSqlTransactionalCommandsContext
	{
		public PostgresDotConnectSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, Transaction transaction)
			: base(sqlDatabaseContext, transaction)
		{
		}

		public override IDbCommand CreateCommand(SqlCreateCommandOptions options)
		{
			var retval = (PgSqlCommand)base.CreateCommand(options);

			if ((options & SqlCreateCommandOptions.UnpreparedExecute) != 0)
			{
				retval.UnpreparedExecute = true;	
			}
			
			return retval;
		}
	}
}
