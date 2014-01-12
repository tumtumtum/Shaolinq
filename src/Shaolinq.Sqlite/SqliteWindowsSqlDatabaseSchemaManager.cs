using System.Data.SQLite;

namespace Shaolinq.Sqlite
{
	public class SqliteWindowsSqlDatabaseSchemaManager
		: SqliteSqlDatabaseSchemaManager
	{
		public SqliteWindowsSqlDatabaseSchemaManager(SqliteSqlDatabaseContext sqlDatabaseContext)
			: base(sqlDatabaseContext)
		{
		}

		protected override void CreateFile(string path)
		{
			SQLiteConnection.CreateFile(path);
		}
	}
}
