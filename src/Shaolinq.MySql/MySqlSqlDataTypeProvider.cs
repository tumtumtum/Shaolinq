// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
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
			DefinePrimitiveSqlDataType(typeof(byte), "TINYINT UNSIGNED", "GetByte");
			DefinePrimitiveSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			DefinePrimitiveSqlDataType(typeof(decimal), "DECIMAL(60, 30)", "GetDecimal");

			DefineSqlDataType(new DateTimeKindNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME", false, DateTimeKind.Utc));
			DefineSqlDataType(new DateTimeKindNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME", true, DateTimeKind.Utc));
		}
	}
}
