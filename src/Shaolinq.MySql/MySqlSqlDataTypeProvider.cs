// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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
			this.DefinePrimitiveSqlDataType(typeof(byte), "TINYINT UNSIGNED", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(decimal), "DECIMAL(60, 30)", "GetDecimal");

			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME", false));
			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME", true));
		}
	}
}
