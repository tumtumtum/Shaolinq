using System;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedTimespanSqlDataType
		: SqlDataType
	{
		private readonly Type underlyingType;

		public PostgresSharedTimespanSqlDataType(ConstraintDefaults constraintDefaults, Type supportedType)
			: base(constraintDefaults, supportedType)
		{
			this.underlyingType = Nullable.GetUnderlyingType(supportedType);
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return "INTERVAL";
		}

		public override Pair<Type, object> ConvertForSql(object value)
		{
			if (value == null)
			{
				return new Pair<Type, object>(typeof(object), null);
			}
			else
			{
				var timespan = (TimeSpan)value;
				var ticks = timespan.Ticks;
				var totalMicroseconds = ticks / 10;
				var microSecondsComponent = totalMicroseconds - (((long)timespan.TotalSeconds) * 1000000);

				var s = string.Format("{0:D2} {1:D2}:{2:D2}:{3:D2}.{4:D6}", timespan.Days, timespan.Hours, Math.Abs(timespan.Minutes), Math.Abs(timespan.Seconds), Math.Abs(microSecondsComponent));

				return new Pair<Type, object>(typeof(object), s);
			}
		}

		public override Expression GetReadExpression(ParameterExpression dataReader, int ordinal)
		{
			if (underlyingType == null)
			{
				return Expression.Convert
				(
					Expression.Call(dataReader, DataRecordMethods.GetValueMethod, Expression.Constant(ordinal)),
					this.SupportedType
				);
			}
			else
			{
				return Expression.Condition
				(
					Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
					Expression.Constant(null, this.SupportedType),
					Expression.Convert
					(
						Expression.Call(dataReader, DataRecordMethods.GetValueMethod, Expression.Constant(ordinal)),
						this.SupportedType
					)
				);
			}
		}
	}
}
