// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shaolinq.Persistence.Sql
{
	public class DefaultSqlDataTypeProvider
		: SqlDataTypeProvider
	{
		public static DefaultSqlDataTypeProvider Instance
		{
			get;
			private set;
		}

		private readonly Dictionary<Type, SqlDataType> sqlDataTypesByType;

		static DefaultSqlDataTypeProvider()
		{
			DefaultSqlDataTypeProvider.Instance = new DefaultSqlDataTypeProvider();
		}

		protected virtual void DefineSqlDataType(SqlDataType sqlDataType)
		{
			sqlDataTypesByType[sqlDataType.SupportedType] = sqlDataType;
		}
        
		protected virtual void DefineSqlDataType(Type type, string name, string getValueMethod)
		{
			DefineSqlDataType(type, name, DataRecordMethods.GetMethod(getValueMethod));
		}

		protected virtual void DefineSqlDataType(Type type, string name, MethodInfo getValueMethod)
		{
			DefineSqlDataType(new PrimitiveSqlDataType(type, name, getValueMethod));
			type = typeof(Nullable<>).MakeGenericType(type);
			DefineSqlDataType(new PrimitiveSqlDataType(type, name, getValueMethod));
		}

		public DefaultSqlDataTypeProvider()
		{
			sqlDataTypesByType = new Dictionary<Type, SqlDataType>(PrimeNumbers.Prime43);

			DefineSqlDataType(typeof(bool), "TINYINT", "GetBoolean");
			DefineSqlDataType(typeof(byte), "UNSIGNED BYTE", "GetByte");
			DefineSqlDataType(typeof(sbyte), "BYTE", "GetByte");
			DefineSqlDataType(typeof(char), "CHAR", "GetChar");
			DefineSqlDataType(typeof(int), "INT", "GetInt32");
			DefineSqlDataType(typeof(uint), "UNSIGNED INT", "GetInt32");
			DefineSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			DefineSqlDataType(typeof(ushort), "UNSIGNED SMALLINT", "GetUInt16");
			DefineSqlDataType(typeof(long), "BIGINT", "GetInt64");
			DefineSqlDataType(typeof(ulong), "UNSIGNED BIGINT", "GetUInt64");
			DefineSqlDataType(typeof(DateTime), "DATETIME", "GetDateTime");
			DefineSqlDataType(typeof(float), "FLOAT", "GetFloat");
			DefineSqlDataType(typeof(double), "DOUBLE", "GetDouble");
			DefineSqlDataType(typeof(decimal), "NUMERIC", "GetDecimal");
			DefineSqlDataType(new DefaultGuidSqlDataType(typeof(Guid)));
			DefineSqlDataType(new DefaultGuidSqlDataType(typeof(Guid?)));
			DefineSqlDataType(new DefaultTimeSpanSqlDataType(this, typeof(TimeSpan)));
			DefineSqlDataType(new DefaultTimeSpanSqlDataType(this, typeof(TimeSpan?)));
			DefineSqlDataType(new DefaultStringSqlDataType());
		}

		protected virtual SqlDataType GetBlobDataType()
		{
			return new DefaultBlobSqlDataType("BLOB");
		}

		public override SqlDataType GetSqlDataType(Type type)
		{
			SqlDataType value;

			if (sqlDataTypesByType.TryGetValue(type, out value))
			{
				return value;
			}

			var newType = type;
			
			if (Nullable.GetUnderlyingType(type) != null)
			{
				newType = Nullable.GetUnderlyingType(type);
			}

			// Support nullable enums
			if (newType.IsEnum)
			{
				var sqlDataType = (SqlDataType)Activator.CreateInstance(typeof(DefaultStringEnumSqlDataType<>).MakeGenericType(type));

				sqlDataTypesByType[type] = sqlDataType;

				return sqlDataType;
			}
			else if (newType.IsArray && newType == typeof(byte[]))
			{
				var sqlDataType = GetBlobDataType();

				sqlDataTypesByType[type] = sqlDataType;

				return sqlDataType;
			}
			else if (newType.IsGenericType && 
				(newType.GetGenericTypeDefinition() == typeof(ShoalinqDictionary<,>)
				|| newType.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
			{
				var args = newType.GetGenericArguments();

				var sqlDataType = (SqlDataType)Activator.CreateInstance(typeof(DefaultDictionarySqlDataType<,>).MakeGenericType(args[0], args[1]), newType);

				sqlDataTypesByType[newType] = sqlDataType;

				return sqlDataType;
			}
			else if (newType.IsGenericType && 
				(newType.GetGenericTypeDefinition() == typeof(ShaolinqList<>)
				|| newType.GetGenericTypeDefinition() == typeof(IList<>)))
			{
				var args = newType.GetGenericArguments();

				var sqlDataType = (SqlDataType)Activator.CreateInstance(typeof(DefaultListSqlDataType<>).MakeGenericType(args[0]), newType);

				sqlDataTypesByType[newType] = sqlDataType;

				return sqlDataType;
			}
			
			throw new NotSupportedException(this.GetType().Name + " does not support " + type.Name);
		}
	}
}
