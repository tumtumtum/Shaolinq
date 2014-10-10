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
		private long shopId;

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

				shop.SecondAddress = this.model.Addresses.Create();
				shop.SecondAddress.Street = "Jefferson Avenue";
				shop.SecondAddress.Region = this.model.Regions.Create();
				shop.SecondAddress.Region.Name = "Washington";
				
				scope.Flush(model);

				shopId = shop.Id;

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
		public void Test_Select_Related_Object()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in model.Shops
					select
						shop.Address;

				var first = query.First();
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject_Two_Levels()
		{
			using (var scope = new TransactionScope())
			{
				var query = from
					shop in model.Shops
					where shop.Id == shopId
					select shop.Include(c => c.Address.Region);

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.Address.Region.IsDeflatedReference);
				Assert.AreEqual("Madison Street", first.Address.Street);
				Assert.AreEqual("Washington", first.Address.Region.Name);
			}
		}

		[Test]
		public void Test_Select_Two_Implicit_Joins_At_Nested_Levels()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Select(c => new { c.Address, c.Address.Region });

				var first = query.First();
			}
		}

		[Test]
		public void Test_Select_Two_Implicit_Joins_At_Same_Level()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Select(c => new
					{
						c.Address,
						c.Mall
					});

				var first = query.First();
			}
		}

		[Test]
		public void Test_Select_Implicit_Join_On_RelatedObject_And_Other_Related_Object_Of_Same_Type1()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Address.Street == "Madison Street" && c.SecondAddress.Street == "Jefferson Avenue")
					.Select(c => c.Include(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsTrue(first.SecondAddress.IsDeflatedReference);
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Implicit_Join_On_RelatedObject_And_Other_Related_Object_Of_Same_Type2()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Address.Street == "Madison Street" )
					.Where(c => c.SecondAddress.Street == "Jefferson Avenue")
					.Select(c => c.Include(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsTrue(first.SecondAddress.IsDeflatedReference);
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Implicit_Join_On_RelatedObject_And_Other_Related_Object_Of_Same_Type3()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Where(c => c.SecondAddress.Street == "Jefferson Avenue")
					.Select(c => c.Include(d => d.Address).Include(d => d.SecondAddress));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference);
				Assert.AreEqual("Madison Street", first.Address.Street);
				Assert.AreEqual("Jefferson Avenue", first.SecondAddress.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(c => c.Include(d => d.Address).Include(d => d.Mall));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.Mall.IsDeflatedReference);
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects2()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(c => c.Include(d => d.Address).Include(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.Address.Region.IsDeflatedReference);
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects3()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(c => c.Include(d => d.Address.Include(e => e.Region)));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.Address.Region.IsDeflatedReference);
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject1()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(c => c.Include(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						shop
					}).Select(c => c.shop.Include(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsTrue(first.Address.Region.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject3()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						shop
					}).Select(c => c.shop.Include(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.Address.Region.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include1()
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
								 shop = shop
							}
						}
					).Select(c => new { shop = c.shop.shop, address = c.shop.shop.Address.Include(d => d.Region) });


				var first = query.First();
				Assert.IsNotNull(first.shop);
				Assert.IsNotNull(first.address);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference);
				Assert.IsFalse(first.shop.Address.Region.IsDeflatedReference);
				Assert.AreEqual(first.shop.Address, first.address);
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include2()
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
							 shop = shop
						 }
					 }
					).Select(c => new { shop = c.shop.shop.Include(d => d.Address.Region), address = c.shop.shop.Address });


				var first = query.First();
				Assert.IsNotNull(first.shop);
				Assert.IsNotNull(first.address);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference);
				Assert.IsFalse(first.shop.Address.Region.IsDeflatedReference);
				Assert.AreEqual(first.shop.Address, first.address);
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include3()
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
							 shop = shop.Include(c => c.Address.Region)
						 }
					 }
					).Select(c => new { shop = c.shop.shop, address = c.shop.shop.Address });


				var first = query.First();
				Assert.IsNotNull(first.shop);
				Assert.IsNotNull(first.address);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference);
				Assert.IsFalse(first.shop.Address.Region.IsDeflatedReference);
				Assert.AreEqual(first.shop.Address, first.address);
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include4()
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
							 shop = shop.Include(c => c.Address)
						 }
					 }
					).Select(c => new { shop = c.shop.shop, address = c.shop.shop.Address });


				var first = query.First();
				Assert.IsNotNull(first.shop);
				Assert.IsNotNull(first.address);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference);
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference);
				Assert.AreEqual(first.shop.Address, first.address);
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include5()
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
							 shop = shop
						 }
					 }
					).Select(c => new { shop = c.shop.shop, address = c.shop.shop.Address });


				var first = query.First();
				Assert.IsNotNull(first.shop);
				Assert.IsNotNull(first.address);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference);
				Assert.AreEqual(first.shop.Address, first.address);
			}
		}

		[Test, Ignore]
		public void Test_Select_Include_Self()
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
							 shop
						 }
					 }
					).Select(c => new { shop = c.shop.shop.Include(d => d) });

				var first = query.First();
				Assert.IsNotNull(first.shop);
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
						 container = new
						 {
							 shop
						 }
					 }
					).Select(c => new { shop = c.container.shop.Include(d => d.Address)});

				var first = query.First();
				Assert.IsNotNull(first.shop);
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
