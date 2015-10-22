// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		private readonly SqlDataType blobSqlDataType;

		public bool NativeUuids { get; set; }
		public bool NativeEnums { get; set; }

		protected override SqlDataType GetBlobDataType()
		{
			return this.blobSqlDataType;
		}

		protected override SqlDataType GetEnumDataType(Type type)
		{
			if (!this.NativeEnums)
			{
				return base.GetEnumDataType(type);
			}

			return new PostgresSharedEnumSqlDataType(this.ConstraintDefaults, type);
		}

		public PostgresSharedSqlDataTypeProvider(ConstraintDefaults constraintDefaults, bool nativeUuids, bool nativeEnums)
			: base(constraintDefaults)
		{
			this.NativeUuids = nativeUuids;
			this.NativeEnums = nativeEnums;
			
			this.blobSqlDataType = new DefaultBlobSqlDataType(constraintDefaults, "BYTEA");

			this.DefineSqlDataType(typeof(bool), "BOOLEAN", "GetBoolean");
			this.DefineSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			this.DefineSqlDataType(typeof(int), "INTEGER", "GetInt32");

			this.DefineSqlDataType(typeof(ushort), "SMALLINT", "GetUInt16");
			this.DefineSqlDataType(typeof(uint), "INTEGER", "GetUInt32");
			this.DefineSqlDataType(typeof(ulong), "BIGINT", "GetUInt64");

			this.DefineSqlDataType(typeof(double), "DOUBLE PRECISION", "GetDouble");
			this.DefineSqlDataType(typeof(byte), "SMALLINT", "GetByte");
			this.DefineSqlDataType(typeof(sbyte), "SMALLINT", "GetByte");
			this.DefineSqlDataType(typeof(decimal), "NUMERIC(57, 28)", "GetDecimal");

			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaults, "TIMESTAMP", false));
			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaults, "TIMESTAMP", true));

			this.DefineSqlDataType(new PostgresSharedTimespanSqlDataType(this.ConstraintDefaults, typeof(TimeSpan)));
			this.DefineSqlDataType(new PostgresSharedTimespanSqlDataType(this.ConstraintDefaults, typeof(TimeSpan?)));

			if (nativeUuids)
			{
				this.DefineSqlDataType(new PostgresSharedUuidSqlDataType(this.ConstraintDefaults, typeof(Guid)));
				this.DefineSqlDataType(new PostgresSharedUuidSqlDataType(this.ConstraintDefaults, typeof(Guid?)));
			}
		}
	}
}
