// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;
using Platform.Reflection;
using Platform.Validation;

namespace Shaolinq.Persistence.Sql
{
	public class DefaultStringSqlDataType
		: SqlDataType
	{
		public DefaultStringSqlDataType()
			: base(typeof(string))
		{
		}

		protected DefaultStringSqlDataType(Type type)
			: base(type)
		{
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return GetSqlName(propertyDescriptor, -1);
		}

		public override long GetDataLength(PropertyDescriptor propertyDescriptor)
		{
			return GetDataLength(propertyDescriptor, -1);
		}

		public virtual long GetDataLength(PropertyDescriptor propertyDescriptor, int hintLength)
		{
			var attribute = propertyDescriptor == null ? null : propertyDescriptor.PersistedMemberAttribute;
			var sizeConstraintAttribute = propertyDescriptor == null ? null : propertyDescriptor.PropertyInfo.GetFirstCustomAttribute<SizeConstraintAttribute>(true);

			if (attribute == null)
			{
				return hintLength < 0 ? SizeConstraintAttribute.DefaultMaximumLength : hintLength;
			}

			if (sizeConstraintAttribute != null)
			{
				switch (sizeConstraintAttribute.SizeFlexibility)
				{
					case SizeFlexibility.Fixed:
						return sizeConstraintAttribute.MaximumLength;
					case SizeFlexibility.Variable:
						return sizeConstraintAttribute.MaximumLength;
					case SizeFlexibility.LargeVariable:
						return Int64.MaxValue;
					default:
						throw new NotSupportedException("SizeFlexibility: " + sizeConstraintAttribute.SizeFlexibility);
				}
			}
			else
			{
				return hintLength < 0 ? SizeConstraintAttribute.DefaultMaximumLength : hintLength;
			}
		}

		protected virtual string GetSqlName(PropertyDescriptor propertyDescriptor, int hintLength)
		{
			var attribute = propertyDescriptor == null ? null : propertyDescriptor.PersistedMemberAttribute;
			var sizeConstraintAttribute = propertyDescriptor == null ? null : propertyDescriptor.PropertyInfo.GetFirstCustomAttribute<SizeConstraintAttribute>(true);
			
			if (attribute == null)
			{
				return "VARCHAR(" + (hintLength < 0 ? SizeConstraintAttribute.DefaultMaximumLength : hintLength) + ") ";
			}

			if (sizeConstraintAttribute != null)
			{
				switch (sizeConstraintAttribute.SizeFlexibility)
				{
					case SizeFlexibility.Fixed:
						return String.Concat("CHAR(", sizeConstraintAttribute.MaximumLength, ")");
					case SizeFlexibility.Variable:
						return String.Concat("VARCHAR(", sizeConstraintAttribute.MaximumLength, ")");
					case SizeFlexibility.LargeVariable:
						return "TEXT";
					default:
						throw new NotSupportedException("SizeFlexibility: " + sizeConstraintAttribute.SizeFlexibility);
				}
			}
			else
			{
				return String.Concat("VARCHAR(", (hintLength < 0 ? SizeConstraintAttribute.DefaultMaximumLength : hintLength), ")");
			}
		}

		public override Expression GetReadExpression(ParameterExpression objectProjector, ParameterExpression dataReader, int ordinal)
		{
			return Expression.Condition
			(
				Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
				Expression.Convert(Expression.Constant(null), this.SupportedType),
				Expression.Call(dataReader, DataRecordMethods.GetStringMethod, Expression.Constant(ordinal))
			);
		}
	}
}
