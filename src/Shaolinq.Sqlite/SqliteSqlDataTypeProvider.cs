// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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
			this.DefinePrimitiveSqlDataType(typeof(byte), "INTEGER", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(sbyte), "INTEGER", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(char), "TEXT", "GetChar");
			this.DefinePrimitiveSqlDataType(typeof(int), "INTEGER", "GetInt32");
			this.DefinePrimitiveSqlDataType(typeof(uint), "INTEGER", "GetInt64");
			this.DefinePrimitiveSqlDataType(typeof(short), "INTEGER", "GetInt16");
			this.DefinePrimitiveSqlDataType(typeof(ushort), "INTEGER", "GetInt32");
			this.DefinePrimitiveSqlDataType(typeof(long), "INTEGER", "GetInt64");
			this.DefinePrimitiveSqlDataType(typeof(ulong), "INTEGER BIGINT", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(DateTime), "TEXT", "GetDateTime");
			this.DefinePrimitiveSqlDataType(typeof(float), "REAL", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(double), "REAL", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(decimal), "TEXT", "GetValue");
		}
	}
}
