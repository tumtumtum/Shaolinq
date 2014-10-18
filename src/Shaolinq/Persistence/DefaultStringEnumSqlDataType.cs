// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using System.Linq;
﻿using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;
﻿using Platform.Validation;

namespace Shaolinq.Persistence
{
	public class DefaultStringEnumSqlDataType<T>
		: DefaultStringSqlDataType
	{
		private static int GetRecommendedLength(Type enumType)
		{
			var type = Nullable.GetUnderlyingType(enumType) ?? enumType;

			var names = Enum.GetNames(type);

			var maximumSize = names.Max(c => c.Length);

			maximumSize = ((maximumSize / 16) + 1) * 16 + 8;

			if (type.GetFirstCustomAttribute<FlagsAttribute>(true) != null)
			{
				maximumSize = (maximumSize + 1) * names.Length;
			}

			return maximumSize;
		}

		private static ConstraintDefaults CreateConstraintDefaults(ConstraintDefaults defaults, Type type)
		{
			var length = GetRecommendedLength(type);

			return new ConstraintDefaults(defaults)
			{
				StringMaximumLength = length,
				IndexedStringMaximumLength = length,
				StringSizeFlexibility = SizeFlexibility.Variable
			};
		}

		public DefaultStringEnumSqlDataType(ConstraintDefaults constraintDefaults)
			: base(CreateConstraintDefaults(constraintDefaults, typeof(T)), typeof(T))
		{
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
							typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new [] { typeof(Type), typeof(string) }, null),
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
							typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new [] { typeof(Type), typeof(string) }, null),
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
	}
}
