// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.MySql
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
