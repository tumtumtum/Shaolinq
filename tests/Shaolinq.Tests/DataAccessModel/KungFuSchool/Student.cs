using System;
using Platform.Validation;

namespace Shaolinq.Tests.DataAccessModel.KungFuSchool
{
	[DataAccessObject]
	public abstract class Student
		: Person
	{
		[PersistedMember]
		public abstract Belt Belt { get; set; }
		
		[BackReference]
		public abstract School School { get; set; }
	}
}
