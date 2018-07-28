// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	public class PostgresUuidSqlDataType
		: SqlDataType
	{
		private static readonly ConstructorInfo NullableGuidConstructor = typeof(Guid?).GetConstructor(new[] { typeof(Guid) });

		public PostgresUuidSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type type)
			: base(constraintDefaultsConfiguration, type)
		{
		}
		
		public override string GetSqlName(PropertyDescriptor propertyDescriptor, ConstraintDefaultsConfiguration constraintDefaults)
		{
			return "UUID";
		}

		public override Expression GetReadExpression(Expression dataReader, int ordinal)
		{
			if (this.UnderlyingType == null)
			{
				return Expression.Condition
				(
					Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
					Expression.Convert(Expression.Constant(this.SupportedType.GetDefaultValue(), this.SupportedType), this.SupportedType),
					Expression.Call(dataReader, DataRecordMethods.GetGuidMethod, Expression.Constant(ordinal))
				);
			}
			else
			{
				return Expression.Condition
				(
					Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
					Expression.Convert(Expression.Constant(null, typeof(Guid?)), this.SupportedType),
					Expression.New
					(
						NullableGuidConstructor,
						Expression.Call(dataReader, DataRecordMethods.GetGuidMethod, Expression.Constant(ordinal))
					)
				);
			}
		}
	}
}
