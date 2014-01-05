// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		private readonly SqlDataType blobSqlDataType;

		protected override SqlDataType GetBlobDataType()
		{
			return blobSqlDataType;
		}

		public PostgresSharedSqlDataTypeProvider(ConstraintDefaults constraintDefaults, bool nativeUuids, DateTimeKind dateTimeKindIfUnspecified)
			: base(constraintDefaults)
		{
			this.blobSqlDataType = new DefaultBlobSqlDataType(constraintDefaults, "BYTEA");

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
				DefineSqlDataType(new PostgresSharedDateTimeDataType(this.ConstraintDefaults, false, dateTimeKindIfUnspecified));
				DefineSqlDataType(new PostgresSharedDateTimeDataType(this.ConstraintDefaults, true, dateTimeKindIfUnspecified));
			}

			if (nativeUuids)
			{
				DefineSqlDataType(new PostgresSharedUuidSqlDataType(this.ConstraintDefaults, typeof(Guid)));
				DefineSqlDataType(new PostgresSharedUuidSqlDataType(this.ConstraintDefaults, typeof(Guid?)));
			}
		}
	}
}
