// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence.Sql.Linq;
using Platform;

namespace Shaolinq.Persistence.Sql
{
	public abstract class SqlDataType
	{
		protected static readonly MethodInfo IsDbNullMethod = DataRecordMethods.IsNullMethod;

		public Type SupportedType { get; private set; }

		/// <summary>
		/// The underlying type if the <see cref="SupportedType"/> is a nullable type.
		/// </summary>
		public Type UnderlyingType { get; private set; }

		public abstract long GetDataLength(PropertyDescriptor propertyDescriptor);

		protected SqlDataType()
		{
		}

		/// <summary>
		/// Converts the given value for serializing to SQL.  The default
		/// implementation performs no conversion.
		/// </summary>
		/// <param name="value">The value</param>
		/// <returns>The converted value</returns>
		public virtual Pair<Type, object> ConvertForSql(object value)
		{
			if (this.UnderlyingType != null)
			{
				return new Pair<Type, object>(this.UnderlyingType, value);
			}
			else
			{
				return new Pair<Type, object>(this.SupportedType, value);
			}
		}

		/// <summary>
		/// Converts a value from SQL to a .NET equivalent.  The default implementation
		/// uses <see cref="Convert.ChangeType(object, Type)"/> and performs <see cref="DBNull"/>
		/// conversion
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <returns>The converted value</returns>
		public virtual object ConvertFromSql(object value)
		{
			if (this.UnderlyingType != null)
			{
				if (value == null || value == DBNull.Value)
				{
					return null;
				}

				return Convert.ChangeType(value, this.UnderlyingType);
			}
			else
			{
				return Convert.ChangeType(value, this.SupportedType);
			}
		}

		/// <summary>
		/// Constructs a new <see cref="SqlDataType"/>
		/// </summary>
		/// <param name="supportedType">The type </param>
		protected SqlDataType(Type supportedType)
		{
			this.SupportedType = supportedType;
			this.UnderlyingType = Nullable.GetUnderlyingType(supportedType);
		}

		/// <summary>
		/// Gets the SQL type name for the given property.
		/// </summary>
		/// <param name="propertyDescriptor">The proeprty whose return type is to be serialized</param>
		/// <returns>The SQL type name</returns>
		public abstract string GetSqlName(PropertyDescriptor propertyDescriptor);

		public virtual string GetMigrationSqlName(PropertyDescriptor propertyDescriptor)
		{
			return GetSqlName(propertyDescriptor);
		}

		/// <summary>
		/// Gets an expression to perform reading of a column.
		/// </summary>
		/// <param name="objectProjector">The parameter that references the <see cref="ObjectProjector"/></param>
		/// <param name="dataReader">The parameter that references the <see cref="IDataReader"/></param>
		/// <param name="ordinal">The parameter that contains the ordinal of the column to read</param>
		/// <returns>An expression for reading the column into a value</returns>
		public abstract Expression GetReadExpression(ParameterExpression objectProjector, ParameterExpression dataReader, int ordinal);
	}
}
