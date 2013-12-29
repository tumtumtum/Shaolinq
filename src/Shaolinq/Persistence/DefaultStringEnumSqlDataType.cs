// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;

namespace Shaolinq.Persistence
{
	public class DefaultStringEnumSqlDataType<T>
		: DefaultStringSqlDataType
	{
		private const int SizePadding = 256;
		private readonly int stringSize = 512;
		
		public DefaultStringEnumSqlDataType()
			: base(typeof(T))
		{
			var type = this.UnderlyingType ?? this.SupportedType;

			var names = Enum.GetNames(type);

			foreach (var name in names)
			{
				while (stringSize - name.Length < SizePadding)
				{
					stringSize *= 2;
				}
			}

			if (type.GetFirstCustomAttribute<FlagsAttribute>(true) != null)
			{
				stringSize = stringSize * Math.Max(1024, names.Length);
			}
		}
        
		public override Expression GetReadExpression(ParameterExpression objectProjector, ParameterExpression dataReader, int ordinal)
		{
			if (this.UnderlyingType == null)
			{
				return Expression.Condition
				(
					Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
					Expression.Constant(Enum.ToObject(this.SupportedType, this.SupportedType.GetDefaultValue()), this.SupportedType),
					Expression.Convert
					(
						Expression.Call
						(
							typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type), typeof(string) }, null),
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
							typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type), typeof(string) }, null),
							Expression.Constant(this.UnderlyingType),
							Expression.Call(dataReader, DataRecordMethods.GetStringMethod, Expression.Constant(ordinal))
						),
						this.SupportedType
					)
				);
			}
		}

		public override Pair<Type, object> ConvertForSql(object value)
		{
			if (value == null)
			{
				return new Pair<Type, object>(typeof(string), value);
			}
			else
			{
				return new Pair<Type, object>(typeof(string), Enum.GetName(this.SupportedType, value));
			}
		}

		public override object ConvertFromSql(object value)
		{
			if (this.UnderlyingType != null)
			{
				if (value == null || value == DBNull.Value)
				{
					return null;
				}
			}

			return Enum.Parse(this.SupportedType, (string)value);
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return base.GetSqlName(propertyDescriptor, this.stringSize);
		}
	}
}
