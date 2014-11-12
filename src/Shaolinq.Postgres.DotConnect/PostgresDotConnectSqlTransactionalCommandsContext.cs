// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Transactions;
using Platform;
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
		
		protected override DbType GetDbType(Type type)
		{
			type = type.GetUnwrappedNullableType();

			if (type == typeof(TimeSpan))
			{
				return DbType.String;
			}

			return base.GetDbType(type);
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
