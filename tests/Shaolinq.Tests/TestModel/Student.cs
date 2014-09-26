// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Student
		: Person
	{
		[PrimaryKey]
		[AutoIncrement]
		[PersistedMember]
		public abstract int SecondaryKey { get; set; }

		[PersistedMember]
		public abstract Sex Sex { get; set; }

		[BackReference, ValueRequired(true)]
		public abstract School School { get; set; }

		[BackReference, ValueRequired(false)]
		public abstract Fraternity Fraternity { get; set; }

		[PersistedMember]
		public abstract Student BestFriend { get; set; }

		[PersistedMember]
		public abstract Address Address { get; set; }
	}
}
