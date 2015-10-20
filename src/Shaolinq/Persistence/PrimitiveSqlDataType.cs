// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
﻿using Platform;

namespace Shaolinq.Persistence
{
	public class PrimitiveSqlDataType
		: SqlDataType
	{
		public string SqlName { get; }
		public MethodInfo GetMethod { get; }

		public PrimitiveSqlDataType(ConstraintDefaults constraintDefaults, Type type, string sqlName, MethodInfo getMethod)
			: base(constraintDefaults, type)
		{
			this.SqlName = sqlName;
			this.GetMethod = getMethod;
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return this.SqlName;
		}

		public static T Read<T>(IDataRecord record, int ordinal)
		{
			return (T)Convert.ChangeType(record.GetValue(ordinal), typeof(T));
		}

		public override Expression GetReadExpression(ParameterExpression dataReader, int ordinal)
		{
			var type = this.UnderlyingType ?? this.SupportedType;

			return Expression.Condition
			(
				Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
				Expression.Convert(Expression.Constant(this.SupportedType.GetDefaultValue()), this.SupportedType),
				Expression.Convert(Expression.Call(null, typeof(PrimitiveSqlDataType).GetMethod("Read", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(type), dataReader, Expression.Constant(ordinal)), this.SupportedType)
			);
		}
	}
}
