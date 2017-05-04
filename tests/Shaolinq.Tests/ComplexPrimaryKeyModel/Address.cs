// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public class Address
		: DataAccessObject<long>
	{
		[PrimaryKey]
		[PersistedMember]
		public virtual Region Region { get; set; }

		[PersistedMember]
		public virtual Region Region2 { get; set; }

		[PersistedMember, DefaultValue(false)]
		public virtual bool Verified { get; set; }

		[PersistedMember, DefaultValue(0)]
		public virtual int Number { get; set; }

		[Index(Condition = "Number != 100 && Street != null && Verified")]
		[Index("Street2_idx", Condition = "Verified == true")]
		[PersistedMember]
		public virtual string Street { get; set; }
	}
}

