// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;
using Shaolinq.Tests.OtherDataAccessObjects;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Student
		: Person, IComparable<Student>
	{
		[PersistedMember]
		public abstract Sex Sex { get; set; }

		[PersistedMember]
		public abstract bool Overseas { get; set; }

		[AutoIncrement]
		[PersistedMember]
		public abstract long SerialNumber1 { get; set; }

		[PersistedMember]
		public abstract Sex? SexOptional { get; set; }

		[BackReference, ValueRequired(true)]
		public abstract School School { get; set; }

		[BackReference, ValueRequired(false)]
		public abstract Fraternity Fraternity { get; set; }

		[Index]
		[PersistedMember, ValueRequired(false)]
		/*[ForeignObjectConstraint(OnDeleteAction = ForeignObjectAction.Restrict, OnUpdateAction = ForeignObjectAction.Restrict)]*/
		public abstract Student BestFriend { get; set; }

		[RelatedDataAccessObjects, ValueRequired(false)]
		public abstract RelatedDataAccessObjects<Cat> Cats { get; }

		[PersistedMember]
		public abstract Address Address { get; set; }

		public int CompareTo(Student other)
		{
			return other == this ? 0 : 1;
		}
	}
}
