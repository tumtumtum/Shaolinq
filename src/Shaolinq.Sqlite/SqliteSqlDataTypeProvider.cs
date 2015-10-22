// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		public SqliteSqlDataTypeProvider(ConstraintDefaults constraintDefaults)
			: base(constraintDefaults)
		{
			this.DefineSqlDataType(typeof(byte), "INTEGER", "GetByte");
			this.DefineSqlDataType(typeof(sbyte), "INTEGER", "GetByte");
			this.DefineSqlDataType(typeof(char), "TEXT", "GetChar");
			this.DefineSqlDataType(typeof(int), "INTEGER", "GetInt32");
			this.DefineSqlDataType(typeof(uint), "INTEGER", "GetInt32");
			this.DefineSqlDataType(typeof(short), "INTEGER", "GetInt16");
			this.DefineSqlDataType(typeof(ushort), "INTEGER", "GetUInt16");
			this.DefineSqlDataType(typeof(long), "INTEGER", "GetInt64");
			this.DefineSqlDataType(typeof(ulong), "INTEGER BIGINT", "GetUInt64");
			this.DefineSqlDataType(typeof(DateTime), "TEXT", "GetDateTime");
			this.DefineSqlDataType(typeof(float), "REAL", "GetFloat");
			this.DefineSqlDataType(typeof(double), "REAL", "GetDouble");
			this.DefineSqlDataType(typeof(decimal), "TEXT", "GetDecimal");
		}
	}
}
