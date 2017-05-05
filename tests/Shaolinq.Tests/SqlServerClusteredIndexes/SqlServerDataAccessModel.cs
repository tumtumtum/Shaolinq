// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Tests.SqlServerClusteredIndexes
{
	[DataAccessModel]
	public abstract class SqlServerDataAccessModel : DataAccessModel
	{
		[DataAccessObjects]
		public abstract DataAccessObjects<Administrator> Administrators { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<DatabaseServer> Servers { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Directory> Directories { get; }
	}
}
