namespace Shaolinq.Persistence.Sql.MySql
{
	public class MySqlSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		public new static MySqlSqlDataTypeProvider Instance { get; private set; }

		static MySqlSqlDataTypeProvider()
		{
			MySqlSqlDataTypeProvider.Instance = new MySqlSqlDataTypeProvider();
		}

		protected override SqlDataType GetBlobDataType()
		{
			return new DefaultBlobSqlDataType("LONGBLOB");
		}

		public MySqlSqlDataTypeProvider()
		{
			DefineSqlDataType(typeof(byte), "UNSIGNED TINYINT", "GetByte");
			DefineSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			DefineSqlDataType(typeof(decimal), "DECIMAL(60, 30)", "GetDecimal");
		}
	}
}
