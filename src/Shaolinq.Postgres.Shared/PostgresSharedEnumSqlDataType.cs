using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedEnumSqlDataType
		: SqlDataType
	{
		private readonly Type underlyingType;

		public PostgresSharedEnumSqlDataType(ConstraintDefaults constraintDefaults, Type supportedType)
			: base(constraintDefaults, supportedType)
		{
			underlyingType = Nullable.GetUnderlyingType(supportedType);
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			var typeDescriptorProvider = propertyDescriptor.DeclaringTypeDescriptor.TypeDescriptorProvider;
			var enumTypeDescriptor = typeDescriptorProvider.GetEnumTypeDescriptor(underlyingType ?? this.SupportedType);

			return enumTypeDescriptor.Name;
		}

		public override Expression GetReadExpression(ParameterExpression objectProjector, ParameterExpression dataReader, int ordinal, bool asObjectKeepNull)
		{
			if (underlyingType == null)
			{
				if (asObjectKeepNull)
				{
					return Expression.Condition
					(
						Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
						Expression.Constant(null, typeof(object)),
						Expression.Convert(Expression.Convert
						(
							Expression.Call
							(
								typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type), typeof(string) }, null),
								Expression.Constant(this.SupportedType),
								Expression.Call(dataReader, DataRecordMethods.GetStringMethod, Expression.Constant(ordinal))
							),
							this.SupportedType
						), typeof(object))
					);	
				}
				else
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
			}
			else
			{
				if (asObjectKeepNull)
				{
					return Expression.Condition
					(
						Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
						Expression.Convert(null, typeof(object)),
						Expression.Convert(Expression.Convert
						(
							Expression.Call
							(
								typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type), typeof(string) }, null),
								Expression.Constant(this.UnderlyingType),
								Expression.Call(dataReader, DataRecordMethods.GetStringMethod, Expression.Constant(ordinal))
							),
							this.SupportedType
						), typeof(object))
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
		}

		public override Pair<Type, object> ConvertForSql(object value)
		{
			if (value == null)
			{
				return new Pair<Type, object>(this.SupportedType, null);
			}
			else
			{
				return new Pair<Type, object>(this.SupportedType, Enum.GetName(this.SupportedType, value));
			}
		}
	}
}
