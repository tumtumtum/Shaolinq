namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessModel]
	public abstract class ComplexPrimaryKeyDataAccessModel
		: DataAccessModel
	{
		[DataAccessObjects]
		public abstract DataAccessObjects<Mall> Malls { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Shop> Shops { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Address> Addresses { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Region> Regions { get; }
	}
}
