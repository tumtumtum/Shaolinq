// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Address
		: DataAccessObject<long>
	{
		[AutoIncrement(Seed = 70000, Step = 1)]
		public abstract override long Id { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract int Number { get; set; }

		[PersistedMember]
		public abstract string Street{ get; set; }

		[PersistedMember]
		public abstract string PostalCode { get; set; }

		[PersistedMember]
		public abstract string State { get; set; }

		[PersistedMember]
		public abstract string Country { get; set; }
	}
}
