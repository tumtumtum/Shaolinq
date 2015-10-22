// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject(NotPersisted = true)]
	public abstract class Person
		: DataAccessObject<Guid>, IIdentified<Guid>
	{
		[PersistedMember]
		public abstract string Firstname { get; set; }

		[Index(LowercaseIndex = true), PersistedMember, SizeConstraint(MaximumLength = 64)]
		public abstract string Email { get; set; }

		[PersistedMember]
		public abstract string Lastname { get; set; }

		[PersistedMember]
		public abstract string Nickname { get; set; }

		[PersistedMember]
		public abstract double Height { get; set; }

		[PersistedMember]
		public abstract double? Weight { get; set; }

		[PersistedMember]
		public abstract int FavouriteNumber { get; set; }

		[PersistedMember]
		[ComputedMember("Height + (Weight ?? 0)")]
		public abstract long HeightAndWeight { get; set; }
        
		[PersistedMember]
		[ComputedMember("CalculateHeightAndWeight()")]
		public abstract long? HeightAndWeight2 { get; set; }

		[DependsOnProperty("Height")]
		[DependsOnProperty("Weight")]
		public long CalculateHeightAndWeight()
		{
			return (int)(this.Height + (this.Weight ?? 0));
		}

		[PersistedMember]
		[ComputedTextMember("{Firstname} {Lastname}")]
		public abstract string Fullname { get; set; }

		[PersistedMember]
		public abstract DateTime? Birthdate { get; set; }

		[PersistedMember]
		public abstract TimeSpan TimeSinceLastSlept { get; set; }

		[DependsOnProperty("Id")]
		protected virtual string CompactIdString => this.Id.ToString("N");

		[PersistedMember]
		[ComputedTextMember("urn:$(TYPENAME_LOWER):{CompactIdString}")]
		public abstract string Urn { get; set; }
	}
}
	
