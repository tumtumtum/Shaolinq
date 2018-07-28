// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Tests.SqlServerClusteredIndexes
{
	[DataAccessObject]
	public class DatabaseServer : DataAccessObject<Guid>
	{
		[OrganizationIndex(Disable = true)]
		public override Guid Id { get; set; }

		[PersistedMember]
		public virtual string Location { get; set; }

		[PersistedMember]
		public virtual int Metric { get; set; }

		[PersistedMember]
		public virtual string Hostname { get; set; }
	}
}