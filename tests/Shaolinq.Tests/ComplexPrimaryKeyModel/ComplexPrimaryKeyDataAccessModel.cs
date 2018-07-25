// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessModel]
	public class ComplexPrimaryKeyDataAccessModel
		: DataAccessModel
	{
		[DataAccessObjects]
		public virtual DataAccessObjects<Mall> Malls { get; set; }

		[DataAccessObjects]
		public virtual DataAccessObjects<Shop> Shops { get; set; }

		[DataAccessObjects]
		public virtual DataAccessObjects<Address> Addresses { get; set; }

		[DataAccessObjects]
		public virtual DataAccessObjects<Region> Regions { get; set; }

		[DataAccessObjects]
		public virtual DataAccessObjects<Coordinate> Coordinates { get; set; }

		[DataAccessObjects]
		public virtual DataAccessObjects<Child> Children { get; set; }

		[DataAccessObjects]
		public virtual DataAccessObjects<Toy> Toys { get; set; }

		[DataAccessObjects]
		public virtual DataAccessObjects<SuperMall> SuperMalls { get; set; }

		[DataAccessObjects]
		public virtual DataAccessObjects<Building> Buildings { get; set; }
	}
}
