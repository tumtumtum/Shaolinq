// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using Shaolinq.Persistence.Sql;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		private static readonly SqlDataType BlobSqlDataType = new DefaultBlobSqlDataType("BYTEA");

		protected override SqlDataType GetBlobDataType()
		{
			return BlobSqlDataType;
		}

		public PostgresSharedSqlDataTypeProvider(bool nativeUuids, DateTimeKind dateTimeKindIfUnspecified)
		{
			DefineSqlDataType(typeof(bool), "BOOLEAN", "GetBoolean");
			DefineSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			DefineSqlDataType(typeof(int), "INTEGER", "GetInt32");
			DefineSqlDataType(typeof(double), "DOUBLE PRECISION", "GetDouble");
			DefineSqlDataType(typeof(byte), "SMALLINT", "GetByte");
			DefineSqlDataType(typeof(sbyte), "SMALLINT", "GetByte");
			DefineSqlDataType(typeof(decimal), "NUMERIC(60, 30)", "GetDecimal");

			if (dateTimeKindIfUnspecified == DateTimeKind.Unspecified)
			{
				DefineSqlDataType(typeof(DateTime), "TIMESTAMP", "GetDateTime");
			}
			else
			{
				DefineSqlDataType(new PostgresSharedDateTimeDataType(false, dateTimeKindIfUnspecified));
				DefineSqlDataType(new PostgresSharedDateTimeDataType(true, dateTimeKindIfUnspecified));
			}

			if (nativeUuids)
			{
				DefineSqlDataType(new PostgresSharedUuidSqlDataType(typeof(Guid)));
				DefineSqlDataType(new PostgresSharedUuidSqlDataType(typeof(Guid?)));
			}
		}
	}
}
