// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedUuidSqlDataType
		: SqlDataType
	{
		private static readonly ConstructorInfo NullableGuidConstructor = typeof(Guid?).GetConstructor(new[] { typeof(Guid) });

		public PostgresSharedUuidSqlDataType(ConstraintDefaults constraintDefaults, Type type)
			: base(constraintDefaults, type)
		{
		}
		
		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return "UUID";
		}

		public override Expression GetReadExpression(ParameterExpression dataReader, int ordinal)
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
