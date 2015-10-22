// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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
			this.DefineSqlDataType(typeof(byte), "TINYINT UNSIGNED", "GetByte");
			this.DefineSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			this.DefineSqlDataType(typeof(decimal), "DECIMAL(60, 30)", "GetDecimal");

			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaults, "DATETIME", false));
			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaults, "DATETIME", true));
		}
	}
}
