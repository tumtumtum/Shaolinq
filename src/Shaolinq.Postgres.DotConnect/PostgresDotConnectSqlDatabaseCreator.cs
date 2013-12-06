// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectSqlDatabaseCreator
		: SqlDatabaseCreator
	{
		public PostgresDotConnectSqlDatabaseCreator(SystemDataBasedDatabaseConnection databaseConnection, DataAccessModel model)
			: base(databaseConnection, model)
		{
		}
	}
}
