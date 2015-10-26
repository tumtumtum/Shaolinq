// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		public SqlServerSqlDataTypeProvider(ConstraintDefaults constraintDefaults)
			: base(constraintDefaults)
		{
			this.DefineSqlDataType(typeof(bool), "BIT", "GetBoolean");
			this.DefineSqlDataType(typeof(byte), "TINYINT", "GetByte");
			this.DefineSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			this.DefineSqlDataType(typeof(char), "CHAR", "GetChar");
			this.DefineSqlDataType(typeof(int), "INT", "GetInt32");
			this.DefineSqlDataType(typeof(uint), "NUMERIC(10)", "GetUInt32");
			this.DefineSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			this.DefineSqlDataType(typeof(ushort), "NUMERIC(5)", "GetUInt16");
			this.DefineSqlDataType(typeof(long), "BIGINT", "GetInt64");
			this.DefineSqlDataType(typeof(ulong), "NUMERIC(20)", "GetUInt64");
			this.DefineSqlDataType(typeof(float), "FLOAT(24)", "GetFloat");
			this.DefineSqlDataType(typeof(double), "FLOAT(53)", "GetDouble");
			
			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaults, "DATETIME2", false));
			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaults, "DATETIME2", true));

			this.DefineSqlDataType(new SqlServerDecimalDataType(constraintDefaults, typeof(decimal), "DECIMAL(38, 9)"));
			this.DefineSqlDataType(new SqlServerDecimalDataType(constraintDefaults, typeof(decimal?), "DECIMAL(38, 9)"));
			this.DefineSqlDataType(new DefaultBlobSqlDataType(constraintDefaults, "VARBINARY(MAX)"));
		}
	}
}
