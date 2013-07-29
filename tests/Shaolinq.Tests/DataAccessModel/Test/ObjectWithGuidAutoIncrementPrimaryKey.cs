using System;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class ObjectWithGuidAutoIncrementPrimaryKey
		: DataAccessObject<Guid>
	{
		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
