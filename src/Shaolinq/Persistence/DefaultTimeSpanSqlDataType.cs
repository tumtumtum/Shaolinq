// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence
{
	public class DefaultTimeSpanSqlDataType
		: SqlDataType
	{
		private readonly SqlDataTypeProvider sqlDataTypeProvider;
		private static readonly MethodInfo getValueMethod = DataRecordMethods.GetInt64Method;
		private static readonly ConstructorInfo timeSpanConstructor = typeof(TimeSpan).GetConstructor(new[] { typeof(long) });
		private static readonly ConstructorInfo nullableTimeSpanConstructor = typeof(TimeSpan?).GetConstructor(new[] { typeof(TimeSpan) });

		public DefaultTimeSpanSqlDataType(SqlDataTypeProvider sqlDataTypeProvider, ConstraintDefaults constraintDefaults, Type type)
			: base(constraintDefaults, type)
		{
			this.sqlDataTypeProvider = sqlDataTypeProvider;
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return this.sqlDataTypeProvider.GetSqlDataType(typeof(long)).GetSqlName(propertyDescriptor);
		}

		public override Pair<Type, object> ConvertForSql(object value)
		{
			if (value == null)
			{
				return new Pair<Type, object>(typeof(long), null);
			}
			else
			{
				return new Pair<Type, object>(typeof(long), ((TimeSpan)value).Ticks);
			}
		}

		public override Expression GetReadExpression(ParameterExpression objectProjector, ParameterExpression dataReader, int ordinal, bool asObjectKeepNull)
		{
			if (Nullable.GetUnderlyingType(this.SupportedType) == null)
			{
				if (asObjectKeepNull)
				{
					return Expression.Condition
					(
						Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
						Expression.Constant(null, typeof(object)),
						Expression.Constant(Expression.New
						(
							timeSpanConstructor,
							Expression.Call(dataReader, getValueMethod, Expression.Constant(ordinal))
						), typeof(object))
					);
				}
				else 
				{ 
					return Expression.Condition
					(
						Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
						Expression.Convert(Expression.Constant(this.SupportedType.GetDefaultValue()), this.SupportedType),
						Expression.New
						(
							timeSpanConstructor,
							Expression.Call(dataReader, getValueMethod, Expression.Constant(ordinal))
						)
					);
				}
			}
			else
			{
				if (asObjectKeepNull)
				{
					return Expression.Condition
					(
						Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
						Expression.Constant(null, typeof(object)),
						Expression.Convert(Expression.New
						(
							nullableTimeSpanConstructor,
							Expression.New
							(
								timeSpanConstructor,
								Expression.Call(dataReader, getValueMethod, Expression.Constant(ordinal))
							)
						), typeof(object))
					);
				}
				else
				{
					return Expression.Condition
					(
						Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
						Expression.Convert(Expression.Constant(this.SupportedType.GetDefaultValue()), this.SupportedType),
						Expression.New
						(
							nullableTimeSpanConstructor,
							Expression.New
							(
								timeSpanConstructor,
								Expression.Call(dataReader, getValueMethod, Expression.Constant(ordinal))
							)
						)
					);
				}
			}
		}
	}
}
