// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Postgres
{
	public class PostgresEnumSqlDataType
		: SqlDataType
	{
		private readonly TypeDescriptorProvider typeDescriptorProvider;
		private readonly Type underlyingType;

		public PostgresEnumSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type supportedType, TypeDescriptorProvider typeDescriptorProvider)
			: base(constraintDefaultsConfiguration, supportedType, true)
		{
			this.typeDescriptorProvider = typeDescriptorProvider;
			this.underlyingType = Nullable.GetUnderlyingType(supportedType);
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor, ConstraintDefaultsConfiguration constraintDefaults)
		{
			var enumTypeDescriptor = this.typeDescriptorProvider.GetEnumTypeDescriptor(this.underlyingType ?? this.SupportedType);

			return enumTypeDescriptor.Name;
		}

		public override Expression GetReadExpression(Expression dataReader, int ordinal)
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

		public override TypedValue ConvertForSql(object value)
		{
			if (value == null)
			{
				return new TypedValue(this.SupportedType, null);
			}
			else
			{
				return new TypedValue(this.SupportedType, Enum.GetName(this.SupportedType.GetUnwrappedNullableType(), value));
			}
		}
	}
}
