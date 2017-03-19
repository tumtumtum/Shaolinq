// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
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

		[DataAccessObjects]
		public abstract DataAccessObjects<Coordinate> Coordinates { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Child> Children { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Toy> Toys { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<SuperMall> SuperMalls {get;}
	}
}
