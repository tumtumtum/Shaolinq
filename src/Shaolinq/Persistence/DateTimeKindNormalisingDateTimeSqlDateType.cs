// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence
{
	public class DateTimeKindNormalisingDateTimeSqlDateType
		: PrimitiveSqlDataType
	{
		private readonly DateTimeKind dateTimeKind;
		private static readonly MethodInfo specifyKindIfUnspecifiedMethod = TypeUtils.GetMethod(() => SpecifyKindIfUnspecified(default(DateTime), default(DateTimeKind)));
		private static readonly MethodInfo specifyKindIfUnspecifiedMethodNullable = TypeUtils.GetMethod(() => SpecifyKindIfUnspecified(default(DateTime?), default(DateTimeKind)));
		
		private readonly MethodInfo specifyKindMethod;

		public DateTimeKindNormalisingDateTimeSqlDateType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, string typeName, bool nullable, DateTimeKind dateTimeKind, MethodInfo dataRecordGetMethod = null)
			: this(constraintDefaultsConfiguration, nullable ? typeof(DateTime?) : typeof(DateTime), typeName, dateTimeKind, dataRecordGetMethod)
		{
		}

		public DateTimeKindNormalisingDateTimeSqlDateType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type type, string typeName, DateTimeKind dateTimeKind, MethodInfo dataRecordGetMethod = null)
			: base(constraintDefaultsConfiguration, type, typeName, dataRecordGetMethod ?? DataRecordMethods.GetMethod(nameof(IDataRecord.GetDateTime)))
		{
			this.dateTimeKind = dateTimeKind;
			this.specifyKindMethod = type.IsNullableType() ? specifyKindIfUnspecifiedMethodNullable : specifyKindIfUnspecifiedMethod;
		}

		public override TypedValue ConvertForSql(object value)
		{
			if (this.UnderlyingType != null)
			{
				value = ((DateTime?)value)?.ToUniversalTime();

				return new TypedValue(this.UnderlyingType, value);
			}
			else
			{
				value = ((DateTime)value).ToUniversalTime();

				return new TypedValue(this.SupportedType, value);
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

			return Expression.Call(this.specifyKindMethod, expression, Expression.Constant(this.dateTimeKind));
		}
	}
}
