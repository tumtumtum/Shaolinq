// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithManyTypes
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string String { get; set; }

		[PersistedMember, DefaultValue("00000000-0000-0000-0000-000000000000")]
		public abstract Guid Guid { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract short Short { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract int Int { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract long Long { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract ushort UShort { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract uint UInt { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract ulong ULong { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract decimal Decimal { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract float Float { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract double Double { get; set; }

		[PersistedMember, DefaultValue(false)]
		public abstract bool Bool { get; set; }

		[PersistedMember, DefaultValue("0001-01-01 00:00")]
		public abstract DateTime DateTime { get; set; }

		[PersistedMember, DefaultValue("00:00")]
		public abstract TimeSpan TimeSpan { get; set; }

		[PersistedMember, DefaultValue(Sex.Male)]
		public abstract Sex Enum { get; set; }

		[PersistedMember]
		public abstract DateTime? NullableDateTime { get; set; }

		[PersistedMember]
		public abstract byte[] ByteArray { get; set; }
	}
}