using Mono.Data.Sqlite;

namespace Shaolinq.Sqlite
{
	public class SqliteMonoSqlDatabaseSchemaManager
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
