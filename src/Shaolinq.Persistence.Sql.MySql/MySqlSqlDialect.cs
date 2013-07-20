namespace Shaolinq.Persistence.Sql.MySql
{
	public class MySqlSqlDialect
		: SqlDialect
	{
		public new static readonly MySqlSqlDialect Default = new MySqlSqlDialect();

		public override string GetAutoIncrementSuffix()
		{
			return "AUTO_INCREMENT";
		}

		public override bool SupportsForUpdate
		{
			get
			{
				return true;
			}
		}
	}
}
