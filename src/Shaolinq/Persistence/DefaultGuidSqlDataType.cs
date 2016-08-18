// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence
{
	public class DefaultGuidSqlDataType
		: SqlDataType
	{
		private static readonly ConstructorInfo GuidConstructor = typeof(Guid).GetConstructor(new [] {typeof(string)});
		private static readonly ConstructorInfo NullableGuidConstructor = typeof(Guid?).GetConstructor(new[] { typeof(Guid) });

		public DefaultGuidSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type type)
			: base(constraintDefaultsConfiguration, type)
		{
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor, ConstraintDefaultsConfiguration constraintDefaults)
		{
			return "CHAR(32)";
		}

		public override TypedValue ConvertForSql(object value)
		{
			if (value == null)
			{
				return new TypedValue(typeof(string), null);
			}
			else
			{
				return new TypedValue(typeof(string), ((Guid)value).ToString("N"));
			}
		}
		
		public static string ReadString(IDataReader reader, int ordinal)
		{
			return reader.GetString(ordinal);
		}

		public override Expression GetReadExpression(Expression dataReader, int ordinal)
		{
			if (this.UnderlyingType == null)
			{
				return Expression.Condition
				(
					Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
					Expression.Convert(Expression.Constant(this.SupportedType.GetDefaultValue(), this.SupportedType), this.SupportedType),
					Expression.New
					(
						GuidConstructor,
						Expression.Call(dataReader, DataRecordMethods.GetStringMethod, Expression.Constant(ordinal))
					)
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
						Expression.New
						(
							GuidConstructor,
							Expression.Call(dataReader, DataRecordMethods.GetStringMethod, Expression.Constant(ordinal))
						)
					)
				);
			}
		}
	}
}
