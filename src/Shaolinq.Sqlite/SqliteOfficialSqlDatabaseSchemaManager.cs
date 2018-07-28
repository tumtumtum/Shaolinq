// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Data.SQLite;

namespace Shaolinq.Sqlite
{
	public partial class SqliteOfficialSqlDatabaseSchemaManager
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
