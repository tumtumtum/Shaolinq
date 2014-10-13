// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public class MySqlSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		protected override SqlDataType GetBlobDataType()
		{
			return new DefaultBlobSqlDataType(this.ConstraintDefaults, "LONGBLOB");
		}

		public MySqlSqlDataTypeProvider(ConstraintDefaults constraintDefaults)
			: base(constraintDefaults)
		{
			DefineSqlDataType(typeof(byte), "TINYINT UNSIGNED", "GetByte");
			DefineSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			DefineSqlDataType(typeof(decimal), "DECIMAL(60, 30)", "GetDecimal");

			DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaults, "DATETIME", false));
			DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaults, "DATETIME", true));
		}
	}
}
