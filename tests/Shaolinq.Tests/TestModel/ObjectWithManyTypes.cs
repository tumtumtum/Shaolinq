using System;
using System.Collections.Generic;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithManyTypes
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string String { get; set; }

		[PersistedMember]
		public abstract Guid Guid { get; set; }

		[PersistedMember]
		public abstract short Short { get; set; }

		[PersistedMember]
		public abstract int Int { get; set; }

		[PersistedMember]
		public abstract long Long { get; set; }

		[PersistedMember]
		public abstract ushort UShort { get; set; }

		[PersistedMember]
		public abstract uint UInt { get; set; }

		[PersistedMember]
		public abstract ulong ULong { get; set; }

		[PersistedMember]
		public abstract decimal Decimal { get; set; }

		[PersistedMember]
		public abstract float Float { get; set; }

		[PersistedMember]
		public abstract double Double { get; set; }

		[PersistedMember]
		public abstract bool Bool { get; set; }

		[PersistedMember]
		public abstract DateTime DateTime { get; set; }

		[PersistedMember]
		public abstract TimeSpan TimeSpan { get; set; }

		[PersistedMember]
		public abstract Sex Enum { get; set; }

		// Formatter doesn't correctly deal with byte array

		//[PersistedMember]
		//public abstract byte[] ByteArray { get; set; }
	}
}