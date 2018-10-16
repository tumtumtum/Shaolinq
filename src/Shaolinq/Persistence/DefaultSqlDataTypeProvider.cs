// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence
{
	public class DefaultSqlDataTypeProvider
		: SqlDataTypeProvider
	{
		private readonly ConcurrentDictionary<Type, SqlDataType> sqlDataTypesByType = new ConcurrentDictionary<Type, SqlDataType>();

		protected internal void DefineSqlDataType(SqlDataType sqlDataType)
		{
			this.sqlDataTypesByType[sqlDataType.SupportedType] = sqlDataType;
		}
		
		protected internal void DefinePrimitiveSqlDataType(Type type, string name, string getValueMethod)
		{
			DefinePrimitiveSqlDataType(type, name, DataRecordMethods.GetMethod(getValueMethod));
		}

		protected internal void DefinePrimitiveSqlDataType(Type type, string name, MethodInfo getValueMethod)
		{
			DefineSqlDataType(new PrimitiveSqlDataType(this.ConstraintDefaultsConfiguration, type, name, getValueMethod));
			type = typeof(Nullable<>).MakeGenericType(type);
			DefineSqlDataType(new PrimitiveSqlDataType(this.ConstraintDefaultsConfiguration, type, name, getValueMethod));
		}

		public DefaultSqlDataTypeProvider(ConstraintDefaultsConfiguration constraintDefaultsConfiguration)
			: base(constraintDefaultsConfiguration)
		{
			DefinePrimitiveSqlDataType(typeof(bool), "TINYINT", "GetBoolean");
			DefinePrimitiveSqlDataType(typeof(byte), "BYTE UNSIGNED ", "GetInt32");
			DefinePrimitiveSqlDataType(typeof(sbyte), "BYTE", "GetByte");
			DefinePrimitiveSqlDataType(typeof(char), "CHAR", "GetChar");
			DefinePrimitiveSqlDataType(typeof(int), "INT", "GetInt32");
			DefinePrimitiveSqlDataType(typeof(uint), "INT UNSIGNED ", "GetInt64");
			DefinePrimitiveSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			DefinePrimitiveSqlDataType(typeof(ushort), "SMALLINT UNSIGNED ", "GetInt32");
			DefinePrimitiveSqlDataType(typeof(long), "BIGINT", "GetInt64");
			DefinePrimitiveSqlDataType(typeof(ulong), "BIGINT UNSIGNED", "GetValue");
			DefinePrimitiveSqlDataType(typeof(DateTime), "DATETIME", "GetDateTime");
			DefinePrimitiveSqlDataType(typeof(float), "FLOAT", "GetFloat");
			DefinePrimitiveSqlDataType(typeof(double), "DOUBLE", "GetDouble");
			DefinePrimitiveSqlDataType(typeof(decimal), "NUMERIC", "GetDecimal");
			DefineSqlDataType(new DefaultGuidSqlDataType(this.ConstraintDefaultsConfiguration, typeof(Guid)));
			DefineSqlDataType(new DefaultGuidSqlDataType(this.ConstraintDefaultsConfiguration, typeof(Guid?)));
			DefineSqlDataType(new DefaultTimeSpanSqlDataType(this, this.ConstraintDefaultsConfiguration, typeof(TimeSpan)));
			DefineSqlDataType(new DefaultTimeSpanSqlDataType(this, this.ConstraintDefaultsConfiguration, typeof(TimeSpan?)));
			DefineSqlDataType(new DefaultStringSqlDataType(this.ConstraintDefaultsConfiguration));
		}

		protected virtual SqlDataType GetBlobDataType()
		{
			return new DefaultBlobSqlDataType(this.ConstraintDefaultsConfiguration, "BLOB");
		}

		protected virtual SqlDataType GetEnumDataType(Type type)
		{
			var sqlDataType = (SqlDataType)Activator.CreateInstance(typeof(DefaultStringEnumSqlDataType<>).MakeGenericType(type), this.ConstraintDefaultsConfiguration);

			return sqlDataType;
		}

		public override SqlDataType GetSqlDataType(Type type)
		{
			if (this.sqlDataTypesByType.TryGetValue(type, out var value))
			{
				return value;
			}

			var underlyingType = type.GetUnwrappedNullableType();
			
			if (underlyingType.IsEnum)
			{
				var sqlDataType = GetEnumDataType(type);

				this.sqlDataTypesByType[type] = sqlDataType;

				return sqlDataType;
			}
			else if (underlyingType.IsArray && underlyingType == typeof(byte[]))
			{
				var sqlDataType = GetBlobDataType();

				this.sqlDataTypesByType[type] = sqlDataType;

				return sqlDataType;
			}

			return null;
		}
	}
}
