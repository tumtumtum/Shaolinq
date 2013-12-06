// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.Postgres
{
	public class PostgresSqlDatabaseCreator
		: SqlDatabaseCreator
	{
		public PostgresSqlDatabaseCreator(SystemDataBasedDatabaseConnection databaseConnection, DataAccessModel model)
			: base(databaseConnection, model)
		{
		}
	}
}
