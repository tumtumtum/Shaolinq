namespace Shaolinq.Persistence.Sql.Sqlite
{
	public class SqliteSqlDialect
		: SqlDialect
	{
		public new static readonly SqliteSqlDialect Default = new SqliteSqlDialect();

		public override bool SupportsConstraints
		{
			get
			{
				return false;
			}
		}

		public override string GetAutoIncrementSuffix()
		{
			return "AUTOINCREMENT";
		}
	}
}
