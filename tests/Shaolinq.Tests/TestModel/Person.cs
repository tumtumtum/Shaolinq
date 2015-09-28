// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject(NotPersisted = true)]
	public abstract class Person
		: DataAccessObject<Guid>
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
		[ComputedMember("Test(Height)")]
		public abstract long HeightAndWeight { get; set; }

		public long Test(double x)
		{
			return 100;
		}

		[PersistedMember]
		[ComputedTextMember("{Firstname} {Lastname}")]
		public abstract string Fullname { get; set; }

		[PersistedMember]
		public abstract DateTime? Birthdate { get; set; }

		[PersistedMember]
		public abstract TimeSpan TimeSinceLastSlept { get; set; }

		[DependsOnProperty("Id")]
		protected virtual string CompactIdString
		{
			get
			{
				return this.Id.ToString("N");
			}
		}

		[PersistedMember]
		[ComputedTextMember("urn:$(TYPENAME_LOWER):{CompactIdString}")]
		public abstract string Urn { get; set; }
	}
}
	
