// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		public SqlServerSqlDataTypeProvider(ConstraintDefaultsConfiguration constraintDefaultsConfiguration)
			: base(constraintDefaultsConfiguration)
		{
			this.DefinePrimitiveSqlDataType(typeof(bool), "BIT", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(byte), "TINYINT", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(char), "CHAR", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(int), "INT", "GetInt32");
			this.DefinePrimitiveSqlDataType(typeof(uint), "NUMERIC(10)", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			this.DefinePrimitiveSqlDataType(typeof(ushort), "NUMERIC(5)", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(long), "BIGINT", "GetInt64");
			this.DefinePrimitiveSqlDataType(typeof(ulong), "NUMERIC(20)", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(float), "FLOAT(24)", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(double), "FLOAT(53)", "GetValue");

			// TODO: Always use GetDateTime when Mono switches to using reference source implementation

			this.DefineSqlDataType(new DateTimeKindNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME2", false, DateTimeKind.Utc,
				SqlServerSqlDatabaseContext.IsRunningMono() ? DataRecordMethods.GetMethod(nameof(IDataRecord.GetValue)) : DataRecordMethods.GetMethod(nameof(IDataRecord.GetDateTime))));
			this.DefineSqlDataType(new DateTimeKindNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME2", true, DateTimeKind.Utc, 
				SqlServerSqlDatabaseContext.IsRunningMono() ? DataRecordMethods.GetMethod(nameof(IDataRecord.GetValue)) : DataRecordMethods.GetMethod(nameof(IDataRecord.GetDateTime))));

			this.DefineSqlDataType(new SqlServerDecimalDataType(constraintDefaultsConfiguration, typeof(decimal), "DECIMAL(38, 9)"));
			this.DefineSqlDataType(new SqlServerDecimalDataType(constraintDefaultsConfiguration, typeof(decimal?), "DECIMAL(38, 9)"));
			this.DefineSqlDataType(new DefaultBlobSqlDataType(constraintDefaultsConfiguration, "VARBINARY(MAX)"));
			this.DefineSqlDataType(new SqlServerStringSqlDataType(constraintDefaultsConfiguration));
		}
	}
}
