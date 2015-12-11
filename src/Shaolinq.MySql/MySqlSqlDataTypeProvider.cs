// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public class MySqlSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		protected override SqlDataType GetBlobDataType()
		{
			return new DefaultBlobSqlDataType(this.ConstraintDefaultsConfiguration, "LONGBLOB");
		}

		public MySqlSqlDataTypeProvider(ConstraintDefaultsConfiguration constraintDefaultsConfiguration)
			: base(constraintDefaultsConfiguration)
		{
			this.DefineSqlDataType(typeof(byte), "TINYINT UNSIGNED", "GetByte");
			this.DefineSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			this.DefineSqlDataType(typeof(decimal), "DECIMAL(60, 30)", "GetDecimal");

			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME", false));
			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME", true));
		}
	}
}
