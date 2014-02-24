// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shaolinq.Persistence
{
	public class DefaultSqlDataTypeProvider
		: SqlDataTypeProvider
	{
		private readonly Dictionary<Type, SqlDataType> sqlDataTypesByType;

		protected void DefineSqlDataType(SqlDataType sqlDataType)
		{
			sqlDataTypesByType[sqlDataType.SupportedType] = sqlDataType;
		}
        
		protected void DefineSqlDataType(Type type, string name, string getValueMethod)
		{
			DefineSqlDataType(type, name, DataRecordMethods.GetMethod(getValueMethod));
		}

		protected void DefineSqlDataType(Type type, string name, MethodInfo getValueMethod)
		{
			DefineSqlDataType(new PrimitiveSqlDataType(this.ConstraintDefaults, type, name, getValueMethod));
			type = typeof(Nullable<>).MakeGenericType(type);
			DefineSqlDataType(new PrimitiveSqlDataType(this.ConstraintDefaults, type, name, getValueMethod));
		}

		public DefaultSqlDataTypeProvider(ConstraintDefaults constraintDefaults)
			: base(constraintDefaults)
		{
			sqlDataTypesByType = new Dictionary<Type, SqlDataType>(PrimeNumbers.Prime43);

			DefineSqlDataType(typeof(bool), "TINYINT", "GetBoolean");
			DefineSqlDataType(typeof(byte), "UNSIGNED BYTE", "GetByte");
			DefineSqlDataType(typeof(sbyte), "BYTE", "GetByte");
			DefineSqlDataType(typeof(char), "CHAR", "GetChar");
			DefineSqlDataType(typeof(int), "INT", "GetInt32");
			DefineSqlDataType(typeof(uint), "UNSIGNED INT", "GetUInt32");
			DefineSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			DefineSqlDataType(typeof(ushort), "UNSIGNED SMALLINT", "GetUInt16");
			DefineSqlDataType(typeof(long), "BIGINT", "GetInt64");
			DefineSqlDataType(typeof(ulong), "UNSIGNED BIGINT", "GetUInt64");
			DefineSqlDataType(typeof(DateTime), "DATETIME", "GetDateTime");
			DefineSqlDataType(typeof(float), "FLOAT", "GetFloat");
			DefineSqlDataType(typeof(double), "DOUBLE", "GetDouble");
			DefineSqlDataType(typeof(decimal), "NUMERIC", "GetDecimal");
			DefineSqlDataType(new DefaultGuidSqlDataType(this.ConstraintDefaults, typeof(Guid)));
			DefineSqlDataType(new DefaultGuidSqlDataType(this.ConstraintDefaults, typeof(Guid?)));
			DefineSqlDataType(new DefaultTimeSpanSqlDataType(this, this.ConstraintDefaults, typeof(TimeSpan)));
			DefineSqlDataType(new DefaultTimeSpanSqlDataType(this, this.ConstraintDefaults, typeof(TimeSpan?)));
			DefineSqlDataType(new DefaultStringSqlDataType(this.ConstraintDefaults));
		}

		protected virtual SqlDataType GetBlobDataType()
		{
			return new DefaultBlobSqlDataType(this.ConstraintDefaults, "BLOB");
		}

		protected virtual SqlDataType GetEnumDataType(Type type)
		{
			var sqlDataType = (SqlDataType)Activator.CreateInstance(typeof(DefaultStringEnumSqlDataType<>).MakeGenericType(type), this.ConstraintDefaults);

			return sqlDataType;
		}

		public override SqlDataType GetSqlDataType(Type type)
		{
			SqlDataType value;

			if (sqlDataTypesByType.TryGetValue(type, out value))
			{
				return value;
			}

			var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
			
			// Support nullable enums
			if (underlyingType.IsEnum)
			{
				var sqlDataType = this.GetEnumDataType(type);

				sqlDataTypesByType[type] = sqlDataType;

				return sqlDataType;
			}
			else if (underlyingType.IsArray && underlyingType == typeof(byte[]))
			{
				var sqlDataType = GetBlobDataType();

				sqlDataTypesByType[type] = sqlDataType;

				return sqlDataType;
			}
			else if (underlyingType.IsGenericType && 
				(underlyingType.GetGenericTypeDefinition() == typeof(ShoalinqDictionary<,>)
				|| underlyingType.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
			{
				var args = underlyingType.GetGenericArguments();

				var sqlDataType = (SqlDataType)Activator.CreateInstance(typeof(DefaultDictionarySqlDataType<,>).MakeGenericType(args[0], args[1]), underlyingType);

				sqlDataTypesByType[underlyingType] = sqlDataType;

				return sqlDataType;
			}
			else if (underlyingType.IsGenericType && 
				(underlyingType.GetGenericTypeDefinition() == typeof(ShaolinqList<>)
				|| underlyingType.GetGenericTypeDefinition() == typeof(IList<>)))
			{
				var args = underlyingType.GetGenericArguments();

				var sqlDataType = (SqlDataType)Activator.CreateInstance(typeof(DefaultListSqlDataType<>).MakeGenericType(args[0]), underlyingType);

				sqlDataTypesByType[underlyingType] = sqlDataType;

				return sqlDataType;
			}
			
			throw new NotSupportedException(this.GetType().Name + " does not support " + type.Name);
		}
	}
}
