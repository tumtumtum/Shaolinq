using System;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class ObjectWithGuidNonAutoIncrementPrimaryKey
		: DataAccessObject<Guid>
	{
		[AutoIncrement(false)]
		public abstract override Guid Id { get; set; }
	}
}
