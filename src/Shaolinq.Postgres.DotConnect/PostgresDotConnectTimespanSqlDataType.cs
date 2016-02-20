// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.DotConnect
{
	internal class PostgresDotConnectTimespanSqlDataType
		: PostgresTimespanSqlDataType
	{
		public PostgresDotConnectTimespanSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type supportedType) : base(constraintDefaultsConfiguration, supportedType)
		{
		}

		public override TypedValue ConvertForSql(object value)
		{
			return new TypedValue(this.SupportedType.GetUnwrappedNullableType(), value);
		}
	}
}
