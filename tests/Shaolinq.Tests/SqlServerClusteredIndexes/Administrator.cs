// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Tests.SqlServerClusteredIndexes
{
	[DataAccessObject]
	public class Administrator : DataAccessObject<Guid>
	{
		[OrganizationIndex(Disable = true)]
		public override Guid Id { get; set; }

		[PersistedMember]
		public virtual string Name { get; set; }
	}
}