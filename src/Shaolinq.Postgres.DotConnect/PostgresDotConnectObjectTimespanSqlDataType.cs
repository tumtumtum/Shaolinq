// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform;
namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectObjectTimespanSqlDataType
		: PostgresTimespanSqlDataType
	{
		public PostgresDotConnectObjectTimespanSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type supportedType)
			: base(constraintDefaultsConfiguration, supportedType)
		{
		}

		public override Tuple<Type, object> ConvertForSql(object value)
		{
			return new Tuple<Type, object>(this.SupportedType.GetUnwrappedNullableType(), value);
		}
	}
}
