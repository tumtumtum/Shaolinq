// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDatabaseCreator
		: SqlDatabaseCreator
	{
		public SqliteSqlDatabaseCreator(SystemDataBasedDatabaseConnection databaseConnection, DataAccessModel model)
			: base(databaseConnection, model)
		{
		}
	}
}
