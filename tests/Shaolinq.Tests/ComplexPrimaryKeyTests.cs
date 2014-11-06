// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;
using Platform;
using Shaolinq.Tests.ComplexPrimaryKeyModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
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
				var region = this.model.Regions.Create();
				region.Name = "Washington";
				region.Diameter = 2000;

				model.Flush();

				var mall = this.model.Malls.Create();

				model.Flush();

				var shop = mall.Shops.Create();

				mall.Name = "Seattle City";

				var address = this.model.Addresses.Create();
				shop.Address = address;
				shop.Address.Street = "Madison Street";
				
				shop.Address.Region = region;
				shop.Name = "Microsoft Store";

				var center = this.model.Coordinates.Create();
				center.Label = "Center of Washington";

				shop.Address.Region.Center = center;
				
				model.Flush();

				region = this.model.Regions.Create();
				shop.SecondAddress = this.model.Addresses.Create();
				shop.SecondAddress.Street = "Jefferson Avenue";
				shop.SecondAddress.Region = region;
				shop.SecondAddress.Region.Name = "Washington";
				
				scope.Flush(model);

				shopId = shop.Id;

				scope.Complete();
			}
		}

		[Test]
		public void Test_Explicit_Complex1()
		{
			using (var scope = new TransactionScope())
			{
				var objs = from toy in model.Toys.Where(c => c.Missing != null).OrderBy(c => c.Name)
						   join child in model.Children.Where(c => c.Nickname != null) on toy.Owner equals child
					where child.Good
					select new
					{
						child,
						toy
					};

				var list = objs.ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Explicit_Complex2()
		{
			using (var scope = new TransactionScope())
			{
				var objs = from toy in model.GetDataAccessObjects<Toy>().Where(c => c.Missing != null).OrderBy(c => c.Name)
						   join child in model.GetDataAccessObjects<Child>().Where(c => c.Nickname != null) on toy.Owner equals child
						   where child.Good
						   select new
						   {
							   child,
							   toy
						   };

				var list = objs.ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Explicit_Join_On_GroupBy()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					(from
						shop in model.Shops
						join address in model.Addresses on shop.Address equals address
						select new
						{
							shop,
							address
						}
						).GroupBy(c => c.address.Street, c => c.shop)
						.Select(c => new { c.Key, count = c.Count()});


				var all = query.ToList();
			}
		}

		[Test]
		public void Test_Implicit_Join_On_GroupBy()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in model.Shops
					group shop by shop.Address.Street
					into g
					select
						g.Key;

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_On_OrderBy()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in model.Shops
					orderby shop.Address.Street
					select
						shop;

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_On_OrderBy_Project_Related_Property()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in model.Shops
					orderby shop.Address.Street
					select
						shop.Address;

				var first = query.First();
			}
		}


		[Test]
		public void Test_Implicit_Join_On_OrderBy_Project_Simple()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in model.Shops
					orderby shop.Address.Street
					select
						shop.Name;

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Where_Join_Not_Primary_Key1()
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
		public void Test_Select_And_Has_Property_With_Null_Value()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Address.Street == "Madison Street");

				var first = query.First();

				Assert.IsNull(first.ThirdAddress);
			}
		}

		[Test]
		public void Test_Include_Property_With_Null_Value()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Include(c => c.ThirdAddress.Region.Center);

				var first = query.First();

				Assert.IsNull(first.ThirdAddress);
			}
		}

		[Test]
		public void Test_Implicit_Join_In_Where_Then_Select_Single_Property()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Select(c => c.Id);

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_In_Where_Then_Project()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Select(c => c);

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_In_Where_Then_Project_Anonymous()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Select(c => new { c });

				var first = query.First();
			}
		}


		[Test]
		public void Test_Include_With_One_Select()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops.Where(c => c.Address.Street == "Madison Street")
					.Select(c => c.Include(d => d.Address).Include(d => d.SecondAddress));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference);
			}
		}
		
		[Test]
		public void Test_Include_With_Two_Different_Selects()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops.Where(c => c.Address.Region.Name == "Washington")
					.Select(c => c.Include(d => d.Address))
					.Select(c => c.Include(d => d.SecondAddress));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Include_With_Two_Different_QuerableIncludes()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops.Where(c => c.Address.Region.Name == "Washington")
					.Include(c => c.Address)
					.Include(c => c.SecondAddress);

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Include_With_Two_Different_QuerableIncludes_Same_Property()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops.Where(c => c.Address.Region.Name == "Washington")
					.Select(c => c.Include(d => d.Address))
					.Select(c => c.Include(d => d.Mall))
					.Select(c => c.Include(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Include_Sample_Property_Twice()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops.Where(c => c.Address.Region.Name == "Washington")
					.Include(c => c.Address)
					.Include(c => c.Address);

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
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
		public void Test_Select_Include_Off_Join()
		{
			using (var scope = new TransactionScope())
			{
				var query = (from  mall  in model.Malls
							join shop in model.Shops on mall equals shop.Mall
							select new {  mall, shop }).Include(c => c.shop.Address);

				var first = query.First();

				Assert.IsFalse(first.shop.IsDeflatedReference);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference);
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
		public void Test_Select_Include_And_Include_RelatedObjects_Via_Pair1()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(c => new Pair<string, Shop>{ Left = "hi", Right = c })
					.Select(c => c.Right.Include(e => e.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.Address.Region.IsDeflatedReference);
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects_Via_Pair2()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(c => new Pair<string, Shop> { Left = "hi", Right = c })
					.Select(c => c.Right.Include(d => d.Address.Include(e => e.Region)));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
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
		public void Test_Select_Include_RelatedObject2a1()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						shop
					}).Include(c => c.shop.Address);

				var first = query.First();

				Assert.IsFalse(first.shop.Address.IsDeflatedReference);
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2a3()
		{
			var y = new
			{
				address = model.Addresses.GetReference(
				new
				{
					Id = 1,
					Region = model.Regions.GetReference(new { Id = 1, Name = "" })
				})
			};

			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						x = new
						{
							y = new
							{
								shop
							}
						}
					}).Include(c => c.x.y.shop.Address.Region);

				var first = query.First();

				Assert.IsFalse(first.x.y.shop.Address.IsDeflatedReference);
				Assert.IsFalse(first.x.y.shop.Address.Region.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2a2()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						x = new
						{
							shop
						}
					}).Include(c => c.x.shop.Address);

				var first = query.First();

				Assert.IsFalse(first.x.shop.Address.IsDeflatedReference);
				Assert.IsTrue(first.x.shop.Address.Region.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2b()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						shop
					}).Select(c => c.Include(d => d.shop.Address));

				var first = query.First();

				Assert.IsFalse(first.shop.Address.IsDeflatedReference);
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2c()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						shop
					}).Where(c => c.shop.Address.Street == "Madison Street");

				var first = query.First();

				Assert.IsTrue(first.shop.Address.IsDeflatedReference);
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2d()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						shop
					}).Select(c => new { c, c.shop, c.shop.Address });

				var first = query.First();

				Assert.IsFalse(first.c.shop.Address.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2e()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						shop
					}).Select(c => new { c.shop, c.shop.Address});

				var first = query.First();
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject3a()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(c => c).Select(c => c.Include(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.Address.Region.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject3b()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						shopp = shop
					}).Select(c => c.shopp.Include(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference);
				Assert.IsFalse(first.Address.Region.IsDeflatedReference);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject3c()
		{
			using (var scope = new TransactionScope())
			{
				var query = model.Shops
					.Where(c => c.Id == shopId)
					.Select(shop => new
					{
						x = new
						{
							shopp = shop
						}
					}).Select(c => c.x.shopp.Include(d => d.Address.Region));

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
							shop1 = new
							{
								 shop2 = shop
							}
						}
					).Select(c => new { shop = c.shop1.shop2, address = c.shop1.shop2.Address.Include(d => d.Region) });


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

		[Test]
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
