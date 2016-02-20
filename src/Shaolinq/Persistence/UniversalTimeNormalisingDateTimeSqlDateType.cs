// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence
{
	public class UniversalTimeNormalisingDateTimeSqlDateType
		: PrimitiveSqlDataType
	{
		private static readonly MethodInfo SpecifyKindIfUnspecifiedMethod = TypeUtils.GetMethod(() => SpecifyKindIfUnspecified(default(DateTime), default(DateTimeKind)));
		private static readonly MethodInfo SpecifyKindIfUnspecifiedMethodNullable = TypeUtils.GetMethod(() => SpecifyKindIfUnspecified(default(DateTime?), default(DateTimeKind)));
		
		private readonly MethodInfo specifyKindMethod;

		public UniversalTimeNormalisingDateTimeSqlDateType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, string typeName, bool nullable)
			: base(constraintDefaultsConfiguration, nullable ? typeof(DateTime?) : typeof(DateTime), typeName, DataRecordMethods.GetMethod("GetDateTime"))
		{
			this.specifyKindMethod = nullable ? SpecifyKindIfUnspecifiedMethodNullable : SpecifyKindIfUnspecifiedMethod;
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

			return Expression.Call(this.specifyKindMethod, expression, Expression.Constant(DateTimeKind.Utc));
		}
	}
}
