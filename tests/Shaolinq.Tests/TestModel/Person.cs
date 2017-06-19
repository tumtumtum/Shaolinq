// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject(NotPersisted = true)]
	public abstract class Person
		: DataAccessObject<Guid>, IIdentified<Guid>
	{
		[PersistedMember]
		[DefaultValue(Value = "Anonymous")]
		public abstract string Firstname { get; set; }

		[Index(LowercaseIndex = true), PersistedMember, SizeConstraint(MaximumLength = 64)]
		public abstract string Email { get; set; }

		[PersistedMember]
		public abstract string Lastname { get; set; }

		[PersistedMember]
		public abstract string Nickname { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract double Height { get; set; }

		[PersistedMember]
		public abstract double? Weight { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract decimal BankBalance { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract int FavouriteNumber { get; set; }
		
		[ComputedMember("Height + (Weight ?? 0)"), DefaultValue(0)]
		public abstract long HeightAndWeight { get; set; }
		
		[Index(Condition = "value != null")]
		[ComputedMember("CalculateHeightAndWeight()")]
		public abstract long? HeightAndWeight2 { get; set; }

		[DependsOnProperty(nameof(Height))]
		[DependsOnProperty(nameof(Weight))]
		public long CalculateHeightAndWeight()
		{
			return (int)(this.Height + (this.Weight ?? 0));
		}
		
		[ComputedTextMember("{Firstname} {Lastname}")]
		public abstract string Fullname { get; set; }

		[PersistedMember, DefaultValue("2016-01-01")]
		public abstract FixedDate? Birthdate { get; set; }

		[PersistedMember]
		public abstract DateTime? SpecialDate { get; set; }

		[PersistedMember, DefaultValue("00:00")]
		public abstract TimeSpan TimeSinceLastSlept { get; set; }

		[DependsOnProperty(nameof(Id))]
		protected virtual string CompactIdString => this.Id.ToString("N");
		
		[ComputedTextMember("urn:$(TABLENAME:L):{Id:N}")]
		public abstract string Urn { get; set; }
	}
}
	
