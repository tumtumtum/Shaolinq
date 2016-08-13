// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using Mono.Data.Sqlite;

namespace Shaolinq.Sqlite
{
	public partial class SqliteMonoSqlDatabaseSchemaManager
		: SqliteSqlDatabaseSchemaManager
	{
		public SqliteMonoSqlDatabaseSchemaManager(SqliteSqlDatabaseContext sqlDatabaseContext)
			: base(sqlDatabaseContext)
		{
		}

		protected override void CreateFile(string path)
		{
			SqliteConnection.CreateFile(path);
		}
	}
}
