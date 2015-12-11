using System;
using Platform;

namespace Shaolinq.Postgres.DotConnect
{
	internal class PostgresDotConnectTimespanSqlDataType
		: PostgresTimespanSqlDataType
	{
		public PostgresDotConnectTimespanSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type supportedType) : base(constraintDefaultsConfiguration, supportedType)
		{
		}

		public override Tuple<Type, object> ConvertForSql(object value)
		{
			return new Tuple<Type, object>(this.SupportedType.GetUnwrappedNullableType(), value);
		}
	}
}
