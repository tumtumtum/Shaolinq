using System;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	public class PostgresTimespanSqlDataType
		: SqlDataType
	{
		private readonly Type underlyingType;

		public PostgresTimespanSqlDataType(ConstraintDefaults constraintDefaults, Type supportedType)
			: base(constraintDefaults, supportedType)
		{
			this.underlyingType = Nullable.GetUnderlyingType(supportedType);
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return "INTERVAL";
		}

		public override Expression GetReadExpression(ParameterExpression dataReader, int ordinal)
		{
			if (this.underlyingType == null)
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

		public override Tuple<Type, object> ConvertForSql(object value)
		{
			if (this.UnderlyingType != null)
			{
				return new Tuple<Type, object>(this.UnderlyingType, value);
			}
			else
			{
				return new Tuple<Type, object>(this.SupportedType, value);
			}
		}
    }
}
