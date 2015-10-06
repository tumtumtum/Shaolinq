// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectSqlDataTypeProvider
		: PostgresSharedSqlDataTypeProvider
	{
		public PostgresDotConnectSqlDataTypeProvider(ConstraintDefaults constraintDefaults, bool nativeUuids, bool nativeEnums, bool objectTimespans)
			: base(constraintDefaults, nativeUuids, nativeEnums)
		{
			if (objectTimespans)
			{
				DefineSqlDataType(new PostgresDotConnectObjectTimespanSqlDataType(this.ConstraintDefaults, typeof(TimeSpan)));
				DefineSqlDataType(new PostgresDotConnectObjectTimespanSqlDataType(this.ConstraintDefaults, typeof(TimeSpan?)));
			}
		}
	}
}
