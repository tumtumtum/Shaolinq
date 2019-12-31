// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		public SqlServerSqlDataTypeProvider(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, bool nativeGuids)
			: base(constraintDefaultsConfiguration)
		{
			DefinePrimitiveSqlDataType(typeof(bool), "BIT", "GetValue");
			DefinePrimitiveSqlDataType(typeof(byte), "TINYINT", "GetByte");
			DefinePrimitiveSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			DefinePrimitiveSqlDataType(typeof(char), "CHAR", "GetValue");
			DefinePrimitiveSqlDataType(typeof(int), "INT", "GetInt32");
			DefinePrimitiveSqlDataType(typeof(uint), "NUMERIC(10)", "GetValue");
			DefinePrimitiveSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			DefinePrimitiveSqlDataType(typeof(ushort), "NUMERIC(5)", "GetValue");
			DefinePrimitiveSqlDataType(typeof(long), "BIGINT", "GetInt64");
			DefinePrimitiveSqlDataType(typeof(ulong), "NUMERIC(20)", "GetValue");
			DefinePrimitiveSqlDataType(typeof(float), "FLOAT(24)", "GetValue");
			DefinePrimitiveSqlDataType(typeof(double), "FLOAT(53)", "GetValue");

			if (nativeGuids)
			{
				DefineSqlDataType(new SqlServerUniqueIdentifierSqlDataType(this.ConstraintDefaultsConfiguration, typeof(Guid)));
				DefineSqlDataType(new SqlServerUniqueIdentifierSqlDataType(this.ConstraintDefaultsConfiguration, typeof(Guid?)));
			}

			// TODO: Always use GetDateTime when Mono switches to using reference source implementation

			DefineSqlDataType(new DateTimeKindNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME2", false, DateTimeKind.Utc,
				SqlServerSqlDatabaseContext.IsRunningMono() ? DataRecordMethods.GetMethod(nameof(IDataRecord.GetValue)) : DataRecordMethods.GetMethod(nameof(IDataRecord.GetDateTime))));
			DefineSqlDataType(new DateTimeKindNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "DATETIME2", true, DateTimeKind.Utc, 
				SqlServerSqlDatabaseContext.IsRunningMono() ? DataRecordMethods.GetMethod(nameof(IDataRecord.GetValue)) : DataRecordMethods.GetMethod(nameof(IDataRecord.GetDateTime))));

			DefineSqlDataType(new SqlServerDecimalDataType(constraintDefaultsConfiguration, typeof(decimal), "DECIMAL(38, 9)"));
			DefineSqlDataType(new SqlServerDecimalDataType(constraintDefaultsConfiguration, typeof(decimal?), "DECIMAL(38, 9)"));
			DefineSqlDataType(new DefaultBlobSqlDataType(constraintDefaultsConfiguration, "VARBINARY(MAX)"));
			DefineSqlDataType(new SqlServerStringSqlDataType(constraintDefaultsConfiguration));
		}
	}
}
