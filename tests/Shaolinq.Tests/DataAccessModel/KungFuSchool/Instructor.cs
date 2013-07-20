using System;

namespace Shaolinq.Tests.DataAccessModel.KungFuSchool
{
	[DataAccessObject]
	public abstract class Instructor
		: DataAccessObject<Guid>
	{
	}
}
