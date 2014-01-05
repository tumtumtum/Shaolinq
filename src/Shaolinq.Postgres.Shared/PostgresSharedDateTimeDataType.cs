// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;
using System.Reflection;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedDateTimeDataType
		: PrimitiveSqlDataType
	{
		private readonly DateTimeKind kind;
		private static readonly MethodInfo SpecifyKindIfUnspecifiedMethod = typeof(PostgresSharedDateTimeDataType).GetMethod("SpecifyKindIfUnspecified", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(DateTime), typeof(DateTimeKind) }, null);

		public PostgresSharedDateTimeDataType(ConstraintDefaults constraintDefaults, bool nullable, DateTimeKind kind)
			: base(constraintDefaults, nullable ? typeof(DateTime?) : typeof(DateTime), "TIMESTAMP", DataRecordMethods.GetMethod("GetDateTime"))
		{
			this.kind = kind;
		}

		public static DateTime SpecifyKindIfUnspecified(DateTime dateTime, DateTimeKind kind)
		{
			return dateTime.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dateTime, kind) : dateTime;
		}

		public override Expression GetReadExpression(ParameterExpression objectProjector, ParameterExpression dataReader, int ordinal)
		{
			var expression = base.GetReadExpression(objectProjector, dataReader, ordinal);

			var retval = Expression.Call(SpecifyKindIfUnspecifiedMethod, expression, Expression.Constant(kind));

			return retval;
		}
	}
}
