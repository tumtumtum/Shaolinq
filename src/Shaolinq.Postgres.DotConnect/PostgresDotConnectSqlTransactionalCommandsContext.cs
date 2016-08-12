// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using Devart.Data.PostgreSql;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectSqlTransactionalCommandsContext
		: DefaultSqlTransactionalCommandsContext
	{
		public PostgresDotConnectSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, IDbConnection connection, DataAccessTransaction transaction)
			: base(sqlDatabaseContext, connection, transaction)
		{
		}

		public override IDbCommand CreateCommand(SqlCreateCommandOptions options)
		{
			var retval = base.CreateCommand(options).Unwrap<PgSqlCommand>();

			if ((options & SqlCreateCommandOptions.UnpreparedExecute) != 0)
			{
				retval.UnpreparedExecute = true;	
			}
			
			return retval;
		}
	}
}
