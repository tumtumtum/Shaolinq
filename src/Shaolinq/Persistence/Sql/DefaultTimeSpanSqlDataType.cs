// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence.Sql
{
	public class DefaultTimeSpanSqlDataType
		: SqlDataType
	{
		private static readonly MethodInfo GetValueMethod = DataRecordMethods.GetInt64Method;
		private static readonly ConstructorInfo TimeSpanConstructor = typeof(TimeSpan).GetConstructor(new[] { typeof(long) });
		private static readonly ConstructorInfo NullableTimeSpanConstructor = typeof(TimeSpan?).GetConstructor(new[] { typeof(TimeSpan) });

		private readonly SqlDataTypeProvider provider;

		public DefaultTimeSpanSqlDataType(SqlDataTypeProvider provider, Type type)
			: base(type)
		{
			this.provider = provider;
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return this.provider.GetSqlDataType(typeof(long)).GetSqlName(propertyDescriptor);
		}

		public override long GetDataLength(PropertyDescriptor propertyDescriptor)
		{
			return 8;
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

		public override Expression GetReadExpression(ParameterExpression objectProjector, ParameterExpression dataReader, int ordinal)
		{
			if (Nullable.GetUnderlyingType(this.SupportedType) == null)
			{
				return Expression.Condition
				(
					Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
					Expression.Convert(Expression.Constant(this.SupportedType.GetDefaultValue()), this.SupportedType),
					Expression.New
					(
						TimeSpanConstructor,
						Expression.Call(dataReader, GetValueMethod, Expression.Constant(ordinal))
					)
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
						NullableTimeSpanConstructor,
						Expression.New
						(
							TimeSpanConstructor,
							Expression.Call(dataReader, GetValueMethod, Expression.Constant(ordinal))
						)
					)
				);
			}
		}
	}
}
