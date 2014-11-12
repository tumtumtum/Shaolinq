using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
