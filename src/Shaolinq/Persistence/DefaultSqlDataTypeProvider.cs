// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence
{
	public class DefaultSqlDataTypeProvider
		: SqlDataTypeProvider
	{
		private readonly Dictionary<Type, SqlDataType> sqlDataTypesByType;

		protected internal void DefineSqlDataType(SqlDataType sqlDataType)
		{
			this.sqlDataTypesByType[sqlDataType.SupportedType] = sqlDataType;
		}
		
		protected internal void DefinePrimitiveSqlDataType(Type type, string name, string getValueMethod)
		{
			this.DefinePrimitiveSqlDataType(type, name, DataRecordMethods.GetMethod(getValueMethod));
		}

		protected internal void DefinePrimitiveSqlDataType(Type type, string name, MethodInfo getValueMethod)
		{
			this.DefineSqlDataType(new PrimitiveSqlDataType(this.ConstraintDefaultsConfiguration, type, name, getValueMethod));
			type = typeof(Nullable<>).MakeGenericType(type);
			this.DefineSqlDataType(new PrimitiveSqlDataType(this.ConstraintDefaultsConfiguration, type, name, getValueMethod));
		}

		public DefaultSqlDataTypeProvider(ConstraintDefaultsConfiguration constraintDefaultsConfiguration)
			: base(constraintDefaultsConfiguration)
		{
			this.sqlDataTypesByType = new Dictionary<Type, SqlDataType>();

			this.DefinePrimitiveSqlDataType(typeof(bool), "TINYINT", "GetBoolean");
			this.DefinePrimitiveSqlDataType(typeof(byte), "BYTE UNSIGNED ", "GetInt32");
			this.DefinePrimitiveSqlDataType(typeof(sbyte), "BYTE", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(char), "CHAR", "GetChar");
			this.DefinePrimitiveSqlDataType(typeof(int), "INT", "GetInt32");
			this.DefinePrimitiveSqlDataType(typeof(uint), "INT UNSIGNED ", "GetInt64");
			this.DefinePrimitiveSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			this.DefinePrimitiveSqlDataType(typeof(ushort), "SMALLINT UNSIGNED ", "GetInt32");
			this.DefinePrimitiveSqlDataType(typeof(long), "BIGINT", "GetInt64");
			this.DefinePrimitiveSqlDataType(typeof(ulong), "BIGINT UNSIGNED", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(DateTime), "DATETIME", "GetDateTime");
			this.DefinePrimitiveSqlDataType(typeof(float), "FLOAT", "GetFloat");
			this.DefinePrimitiveSqlDataType(typeof(double), "DOUBLE", "GetDouble");
			this.DefinePrimitiveSqlDataType(typeof(decimal), "NUMERIC", "GetDecimal");
			this.DefineSqlDataType(new DefaultGuidSqlDataType(this.ConstraintDefaultsConfiguration, typeof(Guid)));
			this.DefineSqlDataType(new DefaultGuidSqlDataType(this.ConstraintDefaultsConfiguration, typeof(Guid?)));
			this.DefineSqlDataType(new DefaultTimeSpanSqlDataType(this, this.ConstraintDefaultsConfiguration, typeof(TimeSpan)));
			this.DefineSqlDataType(new DefaultTimeSpanSqlDataType(this, this.ConstraintDefaultsConfiguration, typeof(TimeSpan?)));
			this.DefineSqlDataType(new DefaultStringSqlDataType(this.ConstraintDefaultsConfiguration));
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
				var sqlDataType = this.GetEnumDataType(type);

				this.sqlDataTypesByType[type] = sqlDataType;

				return sqlDataType;
			}
			else if (underlyingType.IsArray && underlyingType == typeof(byte[]))
			{
				var sqlDataType = this.GetBlobDataType();

				this.sqlDataTypesByType[type] = sqlDataType;

				return sqlDataType;
			}

			return null;
		}
	}
}
