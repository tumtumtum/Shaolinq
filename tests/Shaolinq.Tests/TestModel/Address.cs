// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Address
		: DataAccessObject<long>
	{
		[AutoIncrement(Seed = 70000, Step = 1)]
		public abstract override long Id { get; set; }

		[PersistedMember]
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
