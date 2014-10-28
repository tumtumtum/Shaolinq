using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence
{
	public class UniversalTimeNormalisingDateTimeSqlDateType
		: PrimitiveSqlDataType
	{
		private static readonly MethodInfo specifyKindIfUnspecifiedMethod = typeof(UniversalTimeNormalisingDateTimeSqlDateType).GetMethod("SpecifyKindIfUnspecified", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(DateTime), typeof(DateTimeKind) }, null);
		private static readonly MethodInfo specifyKindIfUnspecifiedMethodNullable = typeof(UniversalTimeNormalisingDateTimeSqlDateType).GetMethod("SpecifyKindIfUnspecified", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(DateTime?), typeof(DateTimeKind) }, null);

		private readonly MethodInfo specifyKindMethod;

		public UniversalTimeNormalisingDateTimeSqlDateType(ConstraintDefaults constraintDefaults, string typeName, bool nullable)
			: base(constraintDefaults, nullable ? typeof(DateTime?) : typeof(DateTime), typeName, DataRecordMethods.GetMethod("GetDateTime"))
		{
			specifyKindMethod = nullable ? specifyKindIfUnspecifiedMethodNullable : specifyKindIfUnspecifiedMethod;
		}

		public override Pair<Type, object> ConvertForSql(object value)
		{
			if (this.UnderlyingType != null)
			{
				if (value != null)
				{
					value = (DateTime?)((DateTime)value).ToUniversalTime();
				}

				return new Pair<Type, object>(this.UnderlyingType, value);
			}
			else
			{
				value = ((DateTime)value).ToUniversalTime();

				return new Pair<Type, object>(this.SupportedType, value);
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

		public override Expression GetReadExpression(ParameterExpression dataReader, int ordinal)
		{
			var expression = base.GetReadExpression(dataReader, ordinal);

			return Expression.Call(specifyKindMethod, expression, Expression.Constant(DateTimeKind.Utc));
		}
	}
}
