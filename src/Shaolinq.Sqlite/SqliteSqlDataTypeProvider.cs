// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		public SqliteSqlDataTypeProvider(ConstraintDefaultsConfiguration constraintDefaultsConfiguration)
			: base(constraintDefaultsConfiguration)
		{
			DefinePrimitiveSqlDataType(typeof(byte), "INTEGER", "GetByte");
			DefinePrimitiveSqlDataType(typeof(sbyte), "INTEGER", "GetByte");
			DefinePrimitiveSqlDataType(typeof(char), "TEXT", "GetChar");
			DefinePrimitiveSqlDataType(typeof(int), "INTEGER", "GetInt32");
			DefinePrimitiveSqlDataType(typeof(uint), "INTEGER", "GetInt64");
			DefinePrimitiveSqlDataType(typeof(short), "INTEGER", "GetInt16");
			DefinePrimitiveSqlDataType(typeof(ushort), "INTEGER", "GetInt32");
			DefinePrimitiveSqlDataType(typeof(long), "INTEGER", "GetInt64");
			DefinePrimitiveSqlDataType(typeof(ulong), "INTEGER BIGINT", "GetValue");
			DefinePrimitiveSqlDataType(typeof(DateTime), "TEXT", "GetDateTime");
			DefinePrimitiveSqlDataType(typeof(float), "REAL", "GetValue");
			DefinePrimitiveSqlDataType(typeof(double), "REAL", "GetValue");
			DefinePrimitiveSqlDataType(typeof(decimal), "TEXT", "GetValue");
		}
	}
}
