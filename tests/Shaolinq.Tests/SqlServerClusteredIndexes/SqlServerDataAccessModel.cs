namespace Shaolinq.Tests.SqlServerClusteredIndexes
{
	[DataAccessModel]
	public abstract class SqlServerDataAccessModel : DataAccessModel
	{
		[DataAccessObjects]
		public abstract DataAccessObjects<Administrator> Administrators { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<DatabaseServer> Servers { get; }
	}
}
