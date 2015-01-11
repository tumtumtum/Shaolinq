using System;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		public SqlServerSqlDataTypeProvider(ConstraintDefaults constraintDefaults)
			: base(constraintDefaults)
		{
			DefineSqlDataType(typeof(bool), "BIT", "GetBoolean");
			DefineSqlDataType(typeof(byte), "TINYINT", "GetByte");
			DefineSqlDataType(typeof(sbyte), "TINYINT", "GetByte");
			DefineSqlDataType(typeof(char), "CHAR", "GetChar");
			DefineSqlDataType(typeof(int), "INT", "GetInt32");
			DefineSqlDataType(typeof(uint), "NUMERIC(10)", "GetUInt32");
			DefineSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			DefineSqlDataType(typeof(ushort), "NUMERIC(5)", "GetUInt16");
			DefineSqlDataType(typeof(long), "BIGINT", "GetInt64");
			DefineSqlDataType(typeof(ulong), "NUMERIC(20)", "GetUInt64");
			DefineSqlDataType(typeof(DateTime), "DATETIME2", "GetDateTime");
			DefineSqlDataType(typeof(float), "FLOAT(24)", "GetFloat");
			DefineSqlDataType(typeof(double), "FLOAT(53)", "GetDouble");
			DefineSqlDataType(typeof(decimal), "DECIMAL", "GetDecimal");
		}
	}
}
