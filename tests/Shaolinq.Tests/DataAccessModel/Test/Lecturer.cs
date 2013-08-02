using System;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class Lecturer
		: DataAccessObject<Guid>
	{
		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Paper> Papers { get; }
	}
}
