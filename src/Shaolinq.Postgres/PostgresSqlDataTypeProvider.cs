// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	public class PostgresSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		private readonly TypeDescriptorProvider typeDescriptorProvider;
		private readonly SqlDataType blobSqlDataType;

		public bool NativeUuids { get; }
		public bool NativeEnums { get; }

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

			return new PostgresEnumSqlDataType(this.ConstraintDefaultsConfiguration, type, this.typeDescriptorProvider);
		}

		public PostgresSqlDataTypeProvider(TypeDescriptorProvider typeDescriptorProvider, ConstraintDefaultsConfiguration constraintDefaultsConfiguration, bool nativeUuids, bool nativeEnums)
			: base(constraintDefaultsConfiguration)
		{
			this.typeDescriptorProvider = typeDescriptorProvider;

			this.NativeUuids = nativeUuids;
			this.NativeEnums = nativeEnums;
			
			this.blobSqlDataType = new DefaultBlobSqlDataType(constraintDefaultsConfiguration, "BYTEA");

			DefinePrimitiveSqlDataType(typeof(bool), "BOOLEAN", "GetBoolean");
			DefinePrimitiveSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			DefinePrimitiveSqlDataType(typeof(int), "INTEGER", "GetInt32");
			DefinePrimitiveSqlDataType(typeof(ushort), "SMALLINT", "GetInt32");
			DefinePrimitiveSqlDataType(typeof(uint), "INTEGER", "GetInt64");
			DefinePrimitiveSqlDataType(typeof(ulong), "BIGINT", "GetValue");
			DefinePrimitiveSqlDataType(typeof(float), "FLOAT(8)", "GetFloat");
			DefinePrimitiveSqlDataType(typeof(double), "DOUBLE PRECISION", "GetDouble");
			DefinePrimitiveSqlDataType(typeof(byte), "SMALLINT", "GetByte");
			DefinePrimitiveSqlDataType(typeof(sbyte), "SMALLINT", "GetByte");
			DefinePrimitiveSqlDataType(typeof(decimal), "NUMERIC(57, 28)", "GetDecimal");

			DefineSqlDataType(new DateTimeKindNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "TIMESTAMP", false, DateTimeKind.Utc));
			DefineSqlDataType(new DateTimeKindNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "TIMESTAMP", true, DateTimeKind.Utc));

			DefineSqlDataType(new PostgresTimespanSqlDataType(this.ConstraintDefaultsConfiguration, typeof(TimeSpan)));
			DefineSqlDataType(new PostgresTimespanSqlDataType(this.ConstraintDefaultsConfiguration, typeof(TimeSpan?)));

			if (nativeUuids)
			{
				DefineSqlDataType(new PostgresUuidSqlDataType(this.ConstraintDefaultsConfiguration, typeof(Guid)));
				DefineSqlDataType(new PostgresUuidSqlDataType(this.ConstraintDefaultsConfiguration, typeof(Guid?)));
			}
		}
	}
}
