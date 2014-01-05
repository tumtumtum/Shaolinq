// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Persistence
{
	public class DefaultBlobSqlDataType
		: SqlDataType
	{
		private static readonly MethodInfo GetBytesMethod = typeof(DefaultBlobSqlDataType).GetMethod("GetBytes"); 
		
		private readonly string sqlName;

		public DefaultBlobSqlDataType(ConstraintDefaults constraintDefaults, string sqlName)
			: base(constraintDefaults, typeof(byte[]))
		{
			this.sqlName = sqlName;
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return sqlName;
		}

		public static byte[] GetBytes(IDataRecord dataRecord, int ordinal)
		{
			var length = dataRecord.GetBytes(ordinal, 0, null, 0, 0);
			
			var buffer = new byte[length];

			int offset = 0;

			while (offset < length)
			{
				offset += (int)dataRecord.GetBytes(ordinal, offset, buffer, offset, (int)length);
			}

			return buffer;
		}

		public override Expression GetReadExpression(ParameterExpression objectProjector, ParameterExpression dataReader, int ordinal)
		{
			return Expression.Condition
			(
				Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
				Expression.Convert(Expression.Constant(null, typeof(byte[])), this.SupportedType),
				Expression.Call(null, GetBytesMethod, dataReader, Expression.Constant(ordinal))
			);
		}
	}
}
