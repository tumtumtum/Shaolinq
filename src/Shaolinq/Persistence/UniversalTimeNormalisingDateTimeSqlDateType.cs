// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence
{
	public class UniversalTimeNormalisingDateTimeSqlDateType
		: PrimitiveSqlDataType
	{
		private static readonly MethodInfo specifyKindIfUnspecifiedMethod = TypeUtils.GetMethod(() => SpecifyKindIfUnspecified(default(DateTime), default(DateTimeKind)));
		private static readonly MethodInfo specifyKindIfUnspecifiedMethodNullable = TypeUtils.GetMethod(() => SpecifyKindIfUnspecified(default(DateTime?), default(DateTimeKind)));
		private static readonly MethodInfo typeConverterConvertToMethod = TypeUtils.GetMethod<TypeConverter>(c => c.ConvertToFix(default(object), typeof(Type)));
		private static readonly MethodInfo typeConverterConvertFromMethod = TypeUtils.GetMethod<TypeConverter>(c => c.ConvertFrom(default(object)));

		private readonly TypeConverter typeConverter;
		private readonly MethodInfo specifyKindMethod;

		public UniversalTimeNormalisingDateTimeSqlDateType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, string typeName, bool nullable)
			: this(constraintDefaultsConfiguration, nullable ? typeof(DateTime?) : typeof(DateTime), typeName)
		{
		}

		public UniversalTimeNormalisingDateTimeSqlDateType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type type, string typeName)
			: base(constraintDefaultsConfiguration, type, typeName, DataRecordMethods.GetMethod(nameof(IDataRecord.GetDateTime)))
		{
			this.specifyKindMethod = type.IsNullableType() ? specifyKindIfUnspecifiedMethodNullable : specifyKindIfUnspecifiedMethod;

			if (this.SupportedType.GetUnwrappedNullableType() != typeof(DateTime))
			{
				this.typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(this.SupportedType);
			}
		}

		public override TypedValue ConvertForSql(object value)
		{
			if (this.UnderlyingType != null)
			{
				if (this.typeConverter != null)
				{
					value = this.typeConverter.ConvertToFix(value, typeof(DateTime?));
				}

				value = ((DateTime?)value)?.ToUniversalTime();

				return new TypedValue(typeof(DateTime), value);
			}
			else
			{
				if (this.typeConverter != null)
				{
					value = this.typeConverter.ConvertToFix(value, typeof(DateTime));
				}

				value = ((DateTime)value).ToUniversalTime();

				return new TypedValue(typeof(DateTime), value);
			}
		}
		
		public static DateTime SpecifyKindIfUnspecified(DateTime dateTime, DateTimeKind kind)
		{
			return dateTime.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dateTime, kind) : dateTime;
		}

		public static DateTime? SpecifyKindIfUnspecified(DateTime? dateTime, DateTimeKind kind)
		{
			if (dateTime == null)
			{
				return null;
			}

			return dateTime.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dateTime.Value, kind) : dateTime;
		}

		public override Expression GetReadExpression(Expression dataReader, int ordinal)
		{
			var expression = base.GetReadExpression(dataReader, ordinal);

			if (expression.Type.GetUnwrappedNullableType() == typeof(DateTime))
			{
				return Expression.Call(this.specifyKindMethod, expression, Expression.Constant(DateTimeKind.Utc));
			}
			else
			{
				return expression;
			}
		}
	}
}
