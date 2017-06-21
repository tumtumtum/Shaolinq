// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence
{
	public class PrimitiveSqlDataType
		: SqlDataType
	{
		public string SqlName { get; }
		public MethodInfo GetMethod { get; }

		public PrimitiveSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type type, string sqlName, MethodInfo getMethod)
			: base(constraintDefaultsConfiguration, type)
		{
			this.SqlName = sqlName;
			this.GetMethod = getMethod;
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor, ConstraintDefaultsConfiguration constraintDefaults)
		{
			return this.SqlName;
		}

		public static T Read<T, U>(Func<U> getValueMethod)
		{
			var value = getValueMethod();

			if (value is T)
			{
				return (T)(object)value;
			}

			if (typeof(IConvertible).IsAssignableFrom(typeof(T)))
			{
				return (T)Convert.ChangeType(value, typeof(T));
			}

			var typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));

			return (T)typeConverter.ConvertFrom(value);
		}

		public override Expression GetReadExpression(Expression dataReader, int ordinal)
		{
			var type = this.UnderlyingType ?? this.SupportedType;

			return Expression.Condition
			(
				Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
				Expression.Convert(Expression.Constant(this.SupportedType.GetDefaultValue()), this.SupportedType),
				Expression.Convert
				(
					Expression.Call
					(
						TypeUtils.GetMethod(() => Read<int, int>(default(Func<int>))).GetGenericMethodDefinition().MakeGenericMethod(type, this.GetMethod.ReturnType),
						Expression.Lambda(Expression.Call(dataReader, this.GetMethod, Expression.Constant(ordinal)))
					),
					this.SupportedType
				)
			);
		}
	}
}
