// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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

		protected void DefineSqlDataType(SqlDataType sqlDataType)
		{
			this.sqlDataTypesByType[sqlDataType.SupportedType] = sqlDataType;
		}
        
		protected void DefineSqlDataType(Type type, string name, string getValueMethod)
		{
			this.DefineSqlDataType(type, name, DataRecordMethods.GetMethod(getValueMethod));
		}

		protected void DefineSqlDataType(Type type, string name, MethodInfo getValueMethod)
		{
			this.DefineSqlDataType(new PrimitiveSqlDataType(this.ConstraintDefaultsConfiguration, type, name, getValueMethod));
			type = typeof(Nullable<>).MakeGenericType(type);
			this.DefineSqlDataType(new PrimitiveSqlDataType(this.ConstraintDefaultsConfiguration, type, name, getValueMethod));
		}

		public DefaultSqlDataTypeProvider(ConstraintDefaultsConfiguration constraintDefaultsConfiguration)
			: base(constraintDefaultsConfiguration)
		{
			this.sqlDataTypesByType = new Dictionary<Type, SqlDataType>();

			this.DefineSqlDataType(typeof(bool), "TINYINT", "GetBoolean");
			this.DefineSqlDataType(typeof(byte), "BYTE UNSIGNED ", "GetByte");
			this.DefineSqlDataType(typeof(sbyte), "BYTE", "GetByte");
			this.DefineSqlDataType(typeof(char), "CHAR", "GetChar");
			this.DefineSqlDataType(typeof(int), "INT", "GetInt32");
			this.DefineSqlDataType(typeof(uint), "INT UNSIGNED ", "GetUInt32");
			this.DefineSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			this.DefineSqlDataType(typeof(ushort), "SMALLINT UNSIGNED ", "GetUInt16");
			this.DefineSqlDataType(typeof(long), "BIGINT", "GetInt64");
			this.DefineSqlDataType(typeof(ulong), "BIGINT UNSIGNED", "GetUInt64");
			this.DefineSqlDataType(typeof(DateTime), "DATETIME", "GetDateTime");
			this.DefineSqlDataType(typeof(float), "FLOAT", "GetFloat");
			this.DefineSqlDataType(typeof(double), "DOUBLE", "GetDouble");
			this.DefineSqlDataType(typeof(decimal), "NUMERIC", "GetDecimal");
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
			SqlDataType value;

			if (this.sqlDataTypesByType.TryGetValue(type, out value))
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
