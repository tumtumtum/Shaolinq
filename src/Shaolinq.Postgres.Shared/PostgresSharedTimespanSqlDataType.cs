using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
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
