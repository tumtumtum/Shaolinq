using System;
using Platform;

namespace Shaolinq.Postgres.DotConnect
{
	internal class PostgresDotConnectTimespanSqlDataType
		: PostgresTimespanSqlDataType
	{
		public PostgresDotConnectTimespanSqlDataType(ConstraintDefaults constraintDefaults, Type supportedType) : base(constraintDefaults, supportedType)
		{
		}

		public override Tuple<Type, object> ConvertForSql(object value)
		{
			return new Tuple<Type, object>(this.SupportedType.GetUnwrappedNullableType(), value);
		}
	}
}
