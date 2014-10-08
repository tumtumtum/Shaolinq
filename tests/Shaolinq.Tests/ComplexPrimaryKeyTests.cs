using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.ComplexPrimaryKeyModel;

namespace Shaolinq.Tests
{
	[TestFixture("Postgres")]
	public class ComplexPrimaryKeyTests
		: BaseTests<ComplexPrimaryKeyDataAccessModel>
	{
		public ComplexPrimaryKeyTests(string providerName)
			: base(providerName)
		{
		}

		[TestFixtureSetUp]
		public void SetUpFixture()
		{
			using (var scope = new TransactionScope())
			{
				var mall = this.model.Malls.Create();
				var shop = mall.Shops.Create();

				mall.Name = "Seattle City";

				shop.Address = this.model.Addresses.Create();
				shop.Address.Street = "Madison Street";
				shop.Address.Region = this.model.Regions.Create();
				shop.Address.Region.Name = "Washington";
				shop.Address.Region.Diameter = 2000;
				shop.Name = "Microsoft Store";

				shop.Address.Region.Center = this.model.Coordinates.Create();
				shop.Address.Region.Center.Label = "Center of Washington";

				scope.Complete();
			}
		}

		[Test]
		public void Test_Implicit_Where_Join_Not_Primary_Key()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in model.Shops
					where
						shop.Address.Street == "Madison Street"
					select
						shop;

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Implicit_Where_Join_Multiple_Depths_Not_Primary_Key()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in model.Shops
					where
						shop.Address.Region.Center.Label == "Center of Washington"
					select
						shop;

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject()
		{
			using (var scope = new TransactionScope())
			{
				var query = from
					shop in model.Shops
					select shop.Include(c => c.Address.Region);

				var first = query.First();

				//Assert.IsFalse(first.Address.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject_Nested_Anonymous()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					(from
						shop in model.Shops
						select new
						{
							shop = new
							{
								 shop = shop.Include(c => c.Address.Include(d => d.Region))
							}
						}
					).Select(c => c.shop.Include(d => d.shop.Address));
					

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Where_Join_Multiple_Depths_Primary_Key()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in model.Shops
					where
						shop.Address.Region.Name == "Washington"
					select
						shop;

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Implicit_Join_With_Complex_Primary_Key()
		{
			using (var scope = new TransactionScope())
			{
				var mall = this.model.Malls.Create();
				var shop = mall.Shops.Create();

				mall.Name = "Westfield";
				
				shop.Address = this.model.Addresses.Create();
				shop.Address.Region = this.model.Regions.Create();
				shop.Address.Region.Name = "City of London";
				shop.Name = "Apple Store";

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				Assert.IsNotNull(this.model.Malls.First(c => c.Name == "Westfield"));
				Assert.IsNotNull(this.model.Shops.FirstOrDefault(c => c.Name == "Apple Store"));
				Assert.IsNotNull(this.model.Shops.First(c => c.Mall.Name == "Westfield"));
				
				var query =
					from
						shop in model.Shops
					where shop.Mall.Name.StartsWith("Westfield")
					select shop;

				Assert.IsNotEmpty(query.ToList());
				
				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Complex_Primary_Key()
		{
			long shopId;
			long addressId;
			long regionId;

			using (var scope = new TransactionScope())
			{
				var shop = this.model.Shops.Create();
				shop.Address = this.model.Addresses.Create();
				shop.Address.Region = this.model.Regions.Create();
				shop.Address.Region.Name = "City of London";

				shop.Name = "Apple Store";

				scope.Flush(model);

				shopId = shop.Id;
				addressId = shop.Address.Id;
				regionId = shop.Address.Region.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var shop = this.model.Shops.FirstOrDefault(c => c.Id == shopId);

				Assert.IsNotNull(shop);
				Assert.IsNotNull(shop.Address);
				Assert.IsNotNull(shop.Address.Region);
				Assert.AreEqual(addressId, shop.Address.Id);
				Assert.AreEqual(regionId, shop.Address.Region.Id);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var shop = this.model.Shops.FirstOrDefault(c => c.Address.Region.Id == regionId);

				Assert.IsNotNull(shop);
				Assert.IsNotNull(shop.Address);
				Assert.IsNotNull(shop.Address.Region);
				Assert.AreEqual(addressId, shop.Address.Id);
				Assert.AreEqual(regionId, shop.Address.Region.Id);

				scope.Complete();
			}
		}
	}
}
