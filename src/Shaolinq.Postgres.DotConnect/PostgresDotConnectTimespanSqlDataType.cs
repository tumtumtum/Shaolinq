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

		public override Pair<Type, object> ConvertForSql(object value)
		{
			return new Pair<Type, object>(this.SupportedType.GetUnwrappedNullableType(), value);
		}
	}
}
