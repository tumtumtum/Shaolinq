// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		public SqliteSqlDataTypeProvider(ConstraintDefaults constraintDefaults)
			: base(constraintDefaults)
		{
			DefineSqlDataType(typeof(byte), "INTEGER", "GetByte");
			DefineSqlDataType(typeof(sbyte), "INTEGER", "GetByte");
			DefineSqlDataType(typeof(char), "TEXT", "GetChar");
			DefineSqlDataType(typeof(int), "INTEGER", "GetInt32");
			DefineSqlDataType(typeof(uint), "INTEGER", "GetInt32");
			DefineSqlDataType(typeof(short), "INTEGER", "GetInt16");
			DefineSqlDataType(typeof(ushort), "INTEGER", "GetUInt16");
			DefineSqlDataType(typeof(long), "INTEGER", "GetInt64");
			DefineSqlDataType(typeof(ulong), "INTEGER BIGINT", "GetUInt64");
			DefineSqlDataType(typeof(DateTime), "TEXT", "GetDateTime");
			DefineSqlDataType(typeof(float), "REAL", "GetFloat");
			DefineSqlDataType(typeof(double), "REAL", "GetDouble");
			DefineSqlDataType(typeof(decimal), "TEXT", "GetDecimal");
		}
	}
}
