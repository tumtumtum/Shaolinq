// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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
			this.DefinePrimitiveSqlDataType(typeof(byte), "TINYINT UNSIGNED", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(decimal), "DECIMAL(60, 30)", "GetDecimal");

			this.DefineSqlDataType(new DateTimeKindNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME", false, DateTimeKind.Utc));
			this.DefineSqlDataType(new DateTimeKindNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME", true, DateTimeKind.Utc));
		}
	}
}
