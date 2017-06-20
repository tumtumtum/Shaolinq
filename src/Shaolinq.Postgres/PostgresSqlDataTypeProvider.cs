// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	public class PostgresSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		private readonly TypeDescriptorProvider typeDescriptorProvider;
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

			return new PostgresEnumSqlDataType(this.ConstraintDefaultsConfiguration, type, this.typeDescriptorProvider);
		}

		public PostgresSqlDataTypeProvider(TypeDescriptorProvider typeDescriptorProvider, ConstraintDefaultsConfiguration constraintDefaultsConfiguration, bool nativeUuids, bool nativeEnums)
			: base(constraintDefaultsConfiguration)
		{
			this.typeDescriptorProvider = typeDescriptorProvider;

			this.NativeUuids = nativeUuids;
			this.NativeEnums = nativeEnums;
			
			this.blobSqlDataType = new DefaultBlobSqlDataType(constraintDefaultsConfiguration, "BYTEA");

			this.DefinePrimitiveSqlDataType(typeof(bool), "BOOLEAN", "GetBoolean");
			this.DefinePrimitiveSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			this.DefinePrimitiveSqlDataType(typeof(int), "INTEGER", "GetInt32");
			this.DefinePrimitiveSqlDataType(typeof(ushort), "SMALLINT", "GetInt32");
			this.DefinePrimitiveSqlDataType(typeof(uint), "INTEGER", "GetInt64");
			this.DefinePrimitiveSqlDataType(typeof(ulong), "BIGINT", "GetValue");
			this.DefinePrimitiveSqlDataType(typeof(float), "FLOAT(8)", "GetFloat");
			this.DefinePrimitiveSqlDataType(typeof(double), "DOUBLE PRECISION", "GetDouble");
			this.DefinePrimitiveSqlDataType(typeof(byte), "SMALLINT", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(sbyte), "SMALLINT", "GetByte");
			this.DefinePrimitiveSqlDataType(typeof(decimal), "NUMERIC(57, 28)", "GetDecimal");

			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "TIMESTAMP", false));
			this.DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaultsConfiguration, "TIMESTAMP", true));

			this.DefineSqlDataType(new PostgresTimespanSqlDataType(this.ConstraintDefaultsConfiguration, typeof(TimeSpan)));
			this.DefineSqlDataType(new PostgresTimespanSqlDataType(this.ConstraintDefaultsConfiguration, typeof(TimeSpan?)));

			if (nativeUuids)
			{
				this.DefineSqlDataType(new PostgresUuidSqlDataType(this.ConstraintDefaultsConfiguration, typeof(Guid)));
				this.DefineSqlDataType(new PostgresUuidSqlDataType(this.ConstraintDefaultsConfiguration, typeof(Guid?)));
			}
		}
	}
}
