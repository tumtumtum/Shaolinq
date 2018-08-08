// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Platform.Reflection;
using Platform.Validation;

namespace Shaolinq.Persistence
{
	public class DefaultStringEnumSqlDataType<T>
		: DefaultStringSqlDataType
	{
		private static int GetRecommendedLength(ConstraintDefaultsConfiguration defaultsConfiguration, Type enumType)
		{
			var type = Nullable.GetUnderlyingType(enumType) ?? enumType;
			
			var names = Enum.GetNames(type);

			if (names.Length == 0)
			{
				return defaultsConfiguration.StringMaximumLength;
			}

			var maximumSize = names.Max(c => c.Length);

			maximumSize = ((maximumSize / 16) + 1) * 16 + 8;

			if (type.GetFirstCustomAttribute<FlagsAttribute>(true) != null)
			{
				maximumSize = (maximumSize + 1) * names.Length;
			}

			return maximumSize;
		}

		private static ConstraintDefaultsConfiguration CreateConstraintDefaults(ConstraintDefaultsConfiguration defaultsConfiguration, Type type)
		{
			var length = GetRecommendedLength(defaultsConfiguration, type);
			var attribute = type.GetUnwrappedNullableType().GetFirstCustomAttribute<SizeConstraintAttribute>(true);
			
			return new ConstraintDefaultsConfiguration(defaultsConfiguration)
			{
				StringMaximumLength = attribute?.MaximumLength > 0 ? attribute.MaximumLength : length,
				IndexedStringMaximumLength = attribute?.MaximumLength > 0 ? attribute.MaximumLength : length,
				StringSizeFlexibility = attribute?.SizeFlexibility ?? SizeFlexibility.Variable
			};
		}

		public DefaultStringEnumSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration)
			: base(CreateConstraintDefaults(constraintDefaultsConfiguration, typeof(T)), typeof(T))
		{
		}
		
		public override Expression GetReadExpression(Expression dataReader, int ordinal)
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
							TypeUtils.GetMethod(() => Enum.Parse(default(Type), default(string))),
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
							TypeUtils.GetMethod(() => Enum.Parse(default(Type), default(string))),
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
				return new TypedValue(typeof(string), value);
			}
			else
			{
				return new TypedValue(typeof(string), Enum.GetName(this.SupportedType.GetUnwrappedNullableType(), value));
			}
		}
	}
}
