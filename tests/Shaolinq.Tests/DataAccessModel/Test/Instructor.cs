using System;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class Instructor
		: DataAccessObject<Guid>
	{
	}
}
