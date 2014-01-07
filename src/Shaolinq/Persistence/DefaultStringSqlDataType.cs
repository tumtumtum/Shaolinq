// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;
using Platform.Reflection;
using Platform.Validation;

namespace Shaolinq.Persistence
{
	public class DefaultStringSqlDataType
		: SqlDataType
	{
		public DefaultStringSqlDataType(ConstraintDefaults constraintDefaults)
			: base(constraintDefaults, typeof(string))
		{
		}

		protected DefaultStringSqlDataType(ConstraintDefaults constraintDefaults, Type type)
			: base(constraintDefaults, type)
		{
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			var sizeConstraintAttribute = propertyDescriptor == null ? null : propertyDescriptor.PropertyInfo.GetFirstCustomAttribute<SizeConstraintAttribute>(true);
			
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
				if (propertyDescriptor.IsPrimaryKey || propertyDescriptor.HasUniqueAttribute || propertyDescriptor.IndexAttributes.Count > 0)
				{
					return "VARCHAR(" + constraintDefaults.IndexedStringMaximumLength + ") ";
				}
				else
				{
					return "VARCHAR(" + constraintDefaults.StringMaximumLength + ") ";
				}
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
