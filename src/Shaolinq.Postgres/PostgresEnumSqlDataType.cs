// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Postgres
{
	internal class PostgresEnumSqlDataType
		: SqlDataType
	{
		private readonly Type underlyingType;

		public PostgresEnumSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type supportedType)
			: base(constraintDefaultsConfiguration, supportedType, true)
		{
			this.underlyingType = Nullable.GetUnderlyingType(supportedType);
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			var typeDescriptorProvider = propertyDescriptor.DeclaringTypeDescriptor.TypeDescriptorProvider;
			var enumTypeDescriptor = typeDescriptorProvider.GetEnumTypeDescriptor(this.underlyingType ?? this.SupportedType);

			return enumTypeDescriptor.Name;
		}

		public override Expression GetReadExpression(ParameterExpression dataReader, int ordinal)
		{
			if (this.underlyingType == null)
			{
				return Expression.Condition
				(
					Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
					Expression.Constant(Enum.ToObject(this.SupportedType, this.SupportedType.GetDefaultValue()), this.SupportedType),
					Expression.Convert
					(
						Expression.Call
						(
							MethodInfoFastRef.EnumParseMethod,
							Expression.Constant(this.SupportedType),
							Expression.Call(dataReader, DataRecordMethods.GetStringMethod, Expression.Constant(ordinal))
						),
						this.SupportedType
					)
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
						Expression.Call
						(
							MethodInfoFastRef.EnumParseMethod,
							Expression.Constant(this.UnderlyingType),
							Expression.Call(dataReader, DataRecordMethods.GetStringMethod, Expression.Constant(ordinal))
						),
						this.SupportedType
					)
				);
			}
		}

		public override Tuple<Type, object> ConvertForSql(object value)
		{
			if (value == null)
			{
				return new Tuple<Type, object>(this.SupportedType, null);
			}
			else
			{
				return new Tuple<Type, object>(this.SupportedType, Enum.GetName(this.SupportedType.GetUnwrappedNullableType(), value));
			}
		}
	}
}
