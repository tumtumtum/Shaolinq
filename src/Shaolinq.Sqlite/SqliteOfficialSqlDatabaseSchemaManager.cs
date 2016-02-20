// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Data.SQLite;

namespace Shaolinq.Sqlite
{
	public class SqliteOfficialSqlDatabaseSchemaManager
		: SqliteSqlDatabaseSchemaManager
	{
		public SqliteOfficialSqlDatabaseSchemaManager(SqliteSqlDatabaseContext sqlDatabaseContext)
			: base(sqlDatabaseContext)
		{
		}

		protected override void CreateFile(string path)
		{
			SQLiteConnection.CreateFile(path);
		}
	}
}
