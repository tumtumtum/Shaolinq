// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.MySql
{
	public class MySqlSqlDatabaseCreator
		: SqlDatabaseCreator
	{
		public MySqlSqlDatabaseCreator(SystemDataBasedDatabaseConnection databaseConnection, DataAccessModel model)
			: base(databaseConnection, model)
		{
		}
	}
}
