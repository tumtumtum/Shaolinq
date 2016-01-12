// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Reflection;
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
	[TestFixture("SqlServer", Category = "IgnoreOnMono")]
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

				this.model.Flush();

				var mall = this.model.Malls.Create();

				this.model.Flush();

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

				this.model.Flush();

				region = this.model.Regions.Create();
				shop.SecondAddress = this.model.Addresses.Create();
				shop.SecondAddress.Street = "Jefferson Avenue";
				shop.SecondAddress.Region = region;
				shop.SecondAddress.Region.Name = "Washington";
				shop.SecondAddress.Region.Diameter = 100;
				
				scope.Flush(this.model);

				this.shopId = shop.Id;

				scope.Complete();
			}
		}
		
		private IQueryable<Address> GetAllAddresses(bool v)
		{
			return this.model.Addresses.Where(c => c.Number != 0);
		}

		private IQueryable<Region> GetAllRegions(bool v)
		{
			return this.model.Regions.Where(c => c.Diameter > 0);
		}

		[Test]
		public void Test_Join_With_Implicit_Join_On_Join_Condition1()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);

			var query = from mall in malls
				join r in GetAllRegions(false) on mall.Address.Region2 equals r
				select new { mall, r };

			var result = query.ToList();
		}
		
		[Test]
		public void Test_Join_With_Implicit_Join_On_Join_Condition2()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);

			var query = from mall in malls
						join r in GetAllRegions(false) on mall.Address.Region.Id equals r.Id
						select new { mall, r };

			var result = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implicit_Join_On_Join_Condition3()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);

			var query = from mall in malls
						join r in GetAllRegions(false) on mall.Address.Region equals r
						select new { mall, r };

			var result = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implicit_Join_On_Join_Condition4()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);

			var query = from mall in malls
						join r in GetAllRegions(false) on mall.Address.Region2 equals r
						select new { mall, r };

			var result = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implicit_Join_On_Join_Condition5()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);

			var query = from mall in malls
						join r in GetAllRegions(false) on mall.Address.Region2.Id equals r.Id
						select new { mall, r };

			var result = query.ToList();
		}

		[Test]
		public void Test_SelectMany1()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);
			
			var query = from mall in malls
				from address in this.model.Addresses.Where(c => c.Number != 0)
				where mall.Address.Region == address.Region
				select new { mall, address };

			query.ToList();
		}

		[Test]
		public void Test_SelectMany2()
		{
			var malls = this.model.Malls;

			var query = from mall in malls
						from address in this.GetAllAddresses(false)
						where mall.Address.Region == address.Region
						select new { mall, address };

			query.ToList();
		}
		
		[Test]
		public void Test_Implicit_Join_Compare_ObjectReferences()
		{
			var malls = this.model.Malls;
			var coordinate = this.model.Coordinates.GetReference(0);

			var query = from mall in malls
						from region in this.model.Regions
						where mall.Address.Region2.Center == coordinate
						select new { mall, region };

			query.ToList();
		}

		[Test]
		public void Test_SuperMall_With_Mall_PrimaryKey()
		{
			Guid mallId;
			long region0, region1, region2;
			long address0, address1, address2;
			
			using (var scope = new TransactionScope())
			{
				var mall = this.model.Malls.Create();
				var superMall = this.model.SuperMalls.Create();

				mall.Address = this.model.Addresses.Create();
				mall.Address.Region = this.model.Regions.Create();
				mall.Address.Region.Name = "!RegionName0";

				superMall.Id = mall;

				superMall.Address1 = this.model.Addresses.Create();
				superMall.Address1.Region = this.model.Regions.Create();
				superMall.Address1.Region.Name = "!RegionName1";

				superMall.Address2 = this.model.Addresses.Create();
				superMall.Address2.Region = this.model.Regions.Create();
				superMall.Address2.Region.Name = "!RegionName2";

				scope.Flush(model);

				mallId = mall.Id;
				address0 = mall.Address.Id;
				address1 = superMall.Address1.Id;
				address2 = superMall.Address2.Id;

				region0 = mall.Address.Region.Id;
				region1 = superMall.Address1.Region.Id;
				region2 = superMall.Address2.Region.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var address = this.model.Addresses.GetReference(new { Id = address0, Region = new { Id = region0, Name = "!RegionName0" } });

				address.Inflate();

				var mall = this.model.Malls.GetReference(new { Id = mallId, Address = new { Id = address0, Region = new { Id = region0, Name = "!RegionName0" } } });

				mall.Inflate();

				var key =
					new
					{
						Id = new { Id = mallId, Address = new { Id = address0, Region = new { Id = region0, Name = "!RegionName0" } } },
						Address1 = new { Id = address1, Region = new { Id = region1, Name = "!RegionName1" } },
						Address2 = new { Id = address2, Region = new { Id = region2, Name = "!RegionName2" } }
					};

				var reference = this.model.SuperMalls.GetReference(key);
				reference.Inflate();

				//var superMall = this.model.SuperMalls.Single(c => c == reference);
			}	
		}

		[Test]
		public void Test_Set_NullableDate()
		{
			using (var scope = new TransactionScope())
			{
				var shop = this.model.Shops.FirstOrDefault(c => c.Name == "Microsoft Store");

				shop.CloseDate = DateTime.Now;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var shop = this.model.Shops.FirstOrDefault(c => c.Name == "Microsoft Store");

				Assert.IsNotNull(shop.CloseDate);

				shop.CloseDate = null;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var shop = this.model.Shops.FirstOrDefault(c => c.Name == "Microsoft Store");

				Assert.IsNull(shop.CloseDate);
			}
		}

		[Test]
		public void Test_Complex_Explicit_Joins()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.SecondAddress equals address2
				select
				new
				{
					shop,
					region1 = address1.Region,
					region2 = address2.Region,
				};

			var results = query.ToList();
		}

		[Test]
		public void Test_Explicit_Left_Join_Empty_Right()
		{
			var query =
				from shop in this.model.Shops
				join address_ in this.model.Addresses on shop.ThirdAddress equals address_ into g
				from address in g.DefaultIfEmpty()
				select
				new
				{
					address
				};

			var first = query.First();

			Assert.IsNull(first.address);
		}

		[Test]
		public void Test_Explicit_Left_Join_Empty_Right2()
		{
			var query =
				from shop in this.model.Shops
				join address in this.model.Addresses on shop.ThirdAddress equals address
				select
				new
				{
					address
				};

			var first = query.FirstOrDefault();

			Assert.IsNull(first);
		}

		[Test]
		public void Test_Complex_Explicit_Joins_With_Explicit_Include_In_Select()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.SecondAddress equals address2
				join
				//address3 in this.model.Addresses on shop.ThirdAddress equals address3 
				_address3 in this.model.Addresses on shop.ThirdAddress equals _address3 into g
				from address3 in g.DefaultIfEmpty()
				select
				new
				{
					shop,
					region1 = address1.Region,
					region2 = address2.Region,
					region3a = address3.Region,
					region3b = address3.Region2,
				};

			var first = query.First();

			Assert.IsFalse(first.region1.IsDeflatedReference());
			Assert.IsFalse(first.region2.IsDeflatedReference());
			Assert.AreEqual(2000, first.region1.Diameter);
			Assert.AreEqual(100, first.region2.Diameter);
			Assert.IsNull(first.region3a);
		}

		[Test]
		public void Test_Complex_Explicit_Joins_With_Back_Reference_On_Condition()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on address1 equals address2
				select
				new
				{
					address1,
					address2
				};

			using (var scope = new TransactionScope())
			{
				var first = query.First();

				Assert.AreSame(first.address1, first.address2);
			}
		}

		[Test]
		public void Test_Complex_Explicit_Joins_Same_Objects()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.Address equals address2
				select
				new
				{
					shop,
					address1,
					address2
				};

			var first = query.First();

			// TODO: Assert.AreSame(first.address1, first.address2);
			Assert.AreEqual(first.address1, first.address2);
		}

		[Test]
		public void Test_Complex_Explicit_Joins_With_Include_In_Queryables()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses.Include(c => c.Region) on shop.SecondAddress equals address2
				join
				_address3 in this.model.Addresses on shop.ThirdAddress equals _address3  into g
				from address3 in g.DefaultIfEmpty()
				join
				address4 in this.model.Addresses.Include(c => c.Region) on shop.SecondAddress equals address4
				join
				address5 in this.model.Addresses.Include(c => c.Region) on address4 equals address5
				select
				new
				{
					shop,
					address1 = address1.Include(c => c.Region.Center),
					address2,
					address3 = address3.Include(c => c.Region),
					address4,
					address5
				};

			using (var scope = new TransactionScope())
			{
				var first = query.First();

				Assert.AreSame(first.address4, first.address5);

				Assert.IsFalse(first.address1.Region.IsDeflatedReference());
				Assert.IsFalse(first.address1.Region.Center.IsDeflatedReference());
				Assert.IsFalse(first.address2.Region.IsDeflatedReference());
				Assert.IsFalse(first.address4.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.address1.Street);
				Assert.AreEqual("Jefferson Avenue", first.address2.Street);
				Assert.AreEqual("Jefferson Avenue", first.address4.Street);
				Assert.AreEqual(2000, first.address1.Region.Diameter);
				Assert.AreEqual(100, first.address2.Region.Diameter);
				Assert.AreEqual(100, first.address4.Region.Diameter);
				Assert.IsNull(first.address3);
			}
		}

		[Test]
		public void Test_Complex_Explicit_Joins_Without_Include_In_Queryables()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.SecondAddress equals address2
				join
				address3_ in this.model.Addresses on shop.ThirdAddress equals address3_ into g
				from address3 in g.DefaultIfEmpty()
				select
				new
				{
					shop,
					address1,
					address2,
					address3
				};

			var first = query.First();

			Assert.IsTrue(first.address1.Region.IsDeflatedReference());
			Assert.IsTrue(first.address2.Region.IsDeflatedReference());
			Assert.AreEqual("Madison Street", first.address1.Street);
			Assert.AreEqual("Jefferson Avenue", first.address2.Street);
			Assert.AreEqual(2000, first.address1.Region.Diameter);
			Assert.AreEqual(100, first.address2.Region.Diameter);
			Assert.IsFalse(first.address1.Region.IsDeflatedReference());
			Assert.IsFalse(first.address2.Region.IsDeflatedReference());
			Assert.IsNull(first.address3);
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_In_Projection()
		{
			var query =
				from
					shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address equals address1
				select new
				{
					address = shop.Address
				};

			var results = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_In_Projection2()
		{
			var query =
				from
					shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address equals address1
				select new
				{
					shop.Address,
					shop.SecondAddress,
					address1.Region
				};

			var results = query.ToList();
		}

		[Test]
		public void Test_Eplicit_Join_With_Implicit_Reference_To_Related_PrimaryKey_In_Projection()
		{
			var query =
				(from
					shop in this.model.Shops
				 join
					 address1 in this.model.Addresses on shop.Address equals address1
				 select new
				 {
					 address1.Region.Name,
					 shop.Address.Id
				 });

			var results = query.ToList();
		}

		[Test]
		public void Test_Explicit_Join_With_Implicit_Reference_To_Related_NonPrimaryKey_In_Projection1()
		{
			var query =
				(from
					shop in this.model.Shops
					join
						address1 in this.model.Addresses on shop.Address equals address1
					select new
					{
						address1.Region.Diameter
					});

			var results = query.ToList();
		}

		[Test]
		public void Test_Eplicit_Join_Project_Into_AnonymousType()
		{
			var query =
				(from
					shop in this.model.Shops
					join
						address1 in this.model.Addresses on shop.Address equals address1
					select new
					{
						A = shop,
						B = address1
					}).Select(c => new
					{
						c.B.Region.Diameter
					});

			var results = query.ToList();
		}

		[Test]
		public void Test_Eplicit_Join_Project_Into_Pair()
		{
			var query =
				(from
					shop in this.model.Shops
				 join
					 address1 in this.model.Addresses on shop.Address equals address1
				 select new Tuple<Shop, Address>(shop, address1)).Select(c => new
				 {
					 c.Item2.Region.Diameter
				 });

			var results = query.ToList();
		}

		[Test]
		public void Test_Eplicit_Join_With_Implicit_Reference_To_Related_NonPrimaryKey_In_Projection2()
		{
			var query =
				(from
					shop in this.model.Shops
				 join
					 address1 in this.model.Addresses on shop.Address equals address1
				 select new
				 {
					 address1.Region.Diameter,
					 shop.Address.Street
				 });

			var results = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_In_Projection3a()
		{
			var query =
				(from
					shop in this.model.Shops
					join
						address1 in this.model.Addresses on shop.Address equals address1
					select new
					{
						address1.Region.Range,
						shop.Address.Street
					});

			var results = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_In_Projection3b()
		{
			var query =
				(from
					shop in this.model.Shops
				 join
					 address1 in this.model.Addresses on shop.Address equals address1
				 select new
				 {
					 shop,
					 address1
				 }).Select(c => new
				 {
					 c.shop.Address.Region.Center,
					 c.address1.Region,
					 c.address1.Region2
				 });

			var results = query.ToList();
		}

		[Test]
		public void Test_Select_Then_Select_With_Multiple_Implicit_Joins()
		{
			var query = (from address in this.model.Addresses
						 select new
						 {
							 address1 = address
						 }).Select
				(c => new
				{
					c.address1.Region,
					c.address1.Region2
				});

			var results = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_In_Projection4()
		{
			var query =
				(from
					shop in this.model.Shops
				 join
					 address1 in this.model.Addresses on shop.Address equals address1
				 select new
				 {
					 shop,
					 address1
				 }).Select(c => new
				 {
					 c.shop.Address,
					 c.address1 
				 }).Select(c => new { c.Address, c.address1.Region });

			var results = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_Two_Deep_In_Projection()
		{
			var query =
				from
					shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address equals address1
				select new
				{
					region = shop.Address.Region
				};

			var results = query.ToList();
		}

		[Test]
		public void Test_Complex_Explicit_And_Implicit_Joins_With_OrderBy()
		{
			var query =
				from
					shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address equals address1
				join
					address2 in this.model.Addresses on shop.SecondAddress equals address2
				orderby shop.Name
				select
					new
					{
						shop,
						region1 = address1.Region,
						address1 = shop.Address
					};

			var results = query.ToList();
		}


		[Test]
		public void Test_Complex_Explicit_With_Includes()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses.Include(c => c.Region) on shop.Address equals address1
				join
				address2 in this.model.Addresses.Include(c => c.Region) on shop.SecondAddress equals address2
				select
				new
				{
					shop,
					region1 = address1.Region,
					region2 = address2.Region2,
				};

			var results = query.ToList();
		}

		[Test, Ignore("Crazy query eh?")]
		public void Test_Explicit_Join_With_Implicit_Join_In_Equality_Properties()
		{
			var query =
				from
				shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address.Region.Diameter equals address1.Region.Diameter
				select
					new
					{
						shop
					};

			var results = query.ToList();
		}

		[Test]
		public void Test_Explicit_Useless_Join_And_Project_Requiring_Implicit_Join()
		{
			var query =
				from
				shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address equals address1
				select
					new
					{
						street = shop.Address.Street
					};

			var results = query.ToList();
		}

		[Test]
		public void Test_Explicit_Useless_Join_And_Project_Requiring_Implicit_Join_Manual_Reselect()
		{
			var query =
				(from
					shop in this.model.Shops
					join
						address1 in this.model.Addresses on shop.Address equals address1
					select
						new
						{
							shop,
							address1
						}
					).Select(c =>
						         new
						         {
							         street = c.shop.Address.Street,
							         diameter = c.shop.Address.Region.Diameter
						         });

			var results = query.ToList();
		}

		[Test]
		public void Test_Complex_Explicit_With_Implicit_Join_In_Projection()
		{
			var query =
				(from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.SecondAddress equals address2
				select
				new
				{
					shop, address1, address2
				})
				.Select(c => 
				new
				{
					c.shop,
					diameter1 = c.address1.Region.Diameter,
					range1 = c.shop.Address.Region.Range
				});

			var results = query.ToList();
		}

		[Test]
		public void Test_Complex_Explicit_And_Implicit_Joins_With_Include_And_OrderBy()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses.Include(c => c.Region) on shop.Address equals address1
				join
				address2 in this.model.Addresses.Include(c => c.Region) on shop.SecondAddress equals address2
				orderby shop.Name
				select
				new
				{
					shop,
					region1 = address1.Region,
					region2 = address2.Region2,
				};

			var results = query.ToList();
		}

		[Test]
		public void Test_Explicit_Complex1()
		{
			using (var scope = new TransactionScope())
			{
				var objs = from toy in this.model.Toys.Where(c => c.Missing != null).OrderBy(c => c.Name)
						   join child in this.model.Children.Where(c => c.Nickname != null) on toy.Owner equals child
					where !child.Good
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
				var objs = from toy in this.model.GetDataAccessObjects<Toy>().Where(c => c.Missing != null).OrderBy(c => c.Name)
						   join child in this.model.GetDataAccessObjects<Child>().Where(c => c.Nickname != null) on toy.Owner equals child
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
		public void Test_Explicit_Join_Select_Then_GroupBy()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
					 join address in this.model.Addresses on shop.Address equals address
					 select new
					 {
						 shop,
						 address
					 }).GroupBy(c => c.address.Street, c => c.shop)
						.Select(c => new { c.Key, count = c.Count() });


				var all = query.ToList();
			}
		}

		[Test]
		public void Test_Explicit_Join_On_GroupBy()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
						join address in this.model.Addresses on shop.Address equals address
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
		public void Test_Implicit_Join_On_GroupBy1()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in this.model.Shops
					group shop by shop.Address.Street
					into g
					select
						g.Key;

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_On_GroupBy2()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops.GroupBy(c => c.Address.Street, c => new { Number = 1, Key = c.Address });

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
						shop in this.model.Shops
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
						shop in this.model.Shops
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
						shop in this.model.Shops
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
						shop in this.model.Shops
					where
						shop.Address.Street == "Madison Street"
					select
						shop;

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_And_Has_Property_With_Null_Value()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
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
				var query = this.model.Shops
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
				var query = this.model.Shops
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
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Select(c => c);

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project1()
		{
			var results = this.model.Shops.Where(c => c.Mall.Address.Id == 0).ToList();
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project2()
		{
			var results = this.model.Shops.Where(c => c.Mall.Id == Guid.Empty).ToList();
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project3()
		{
			var results = this.model.Shops.Include(c => c.Mall).Where(c => c.Mall.Address.Id == 0).ToList();
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project4()
		{
			var results = this.model.Shops.Select(c => c.Mall.Address.Id == 0).ToList();
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project4b()
		{
			var results = this.model.Shops.Include(c => c.Mall).Select(c => c.Mall.Address.Id == 0).ToList();
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project5()
		{
			var results = this.model.Shops.Select(c => c.Mall.Address.Street == null).ToList();
		}

		[Test]
		public void Test_Twin_Implicit_Join_In_Where_Then_Project()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Name != "" && c.OpeningDate > new DateTime())
					.Where(c => c.Address.Street == "Madison Street"
						&& c.SecondAddress.Number == 0
						&& c.ThirdAddress.Number == 0
						&& c.ThirdAddress.Region.Diameter >= 0)
					.Select(c => c);

				var values = query.ToList();
			}
		}

		[Test]
		public void Test_Implicit_Join_From_Projection()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Select(c => new { shop = c, region = c.Address.Region, region2 = c.SecondAddress.Region })
					.Where(c => c.shop.Name != null && c.region2.Diameter >= 0 && c.region.Diameter >= 0);

				var values = query.ToList();
			}
		}

		[Test]
		public void Test_Implicit_Join_In_Where_Then_Project_Anonymous()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
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
				var query = this.model.Shops.Where(c => c.Address.Street == "Madison Street")
					.Select(c => c.Include(d => d.Address).Include(d => d.SecondAddress));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Same_Property_Sub_Property_Before()
		{
			using (var scope = new TransactionScope())
			{
#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
				var query = this.model.Shops
					.Where(c => c.SecondAddress.Number != null)
					.Include(c => c.SecondAddress);
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Same_Property_Before()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.SecondAddress != null)
					.Include(c => c.SecondAddress);

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Same_Property_Afterwards()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Include(c => c.SecondAddress)
					.Where(c => c.SecondAddress != null);

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Parent_Value_Afterwards()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Include(c => c.SecondAddress)
					.Where(c => c != null);

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Different_Object_Property_Afterwards()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address != null)
					.Include(c => c.SecondAddress);

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_Complex_Key_Is_Not_Null()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address != null);

				var first = query.First();

				Assert.IsNotNull(first.Address);
				Assert.IsTrue(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_Complex_Key_Is_Null()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address == null);

				var first = query.FirstOrDefault();

				Assert.IsNull(first);
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Different_Property_Afterwards()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Include(c => c.SecondAddress)
					.Where(c => c.Address != null);

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}
		
		[Test]
		public void Test_Include_With_Two_Different_Selects()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops.Where(c => c.Address.Region.Name == "Washington")
					.Select(c => c.Include(d => d.Address))
					.Select(c => c.Include(d => d.SecondAddress));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Two_Different_QuerableIncludes()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops.Where(c => c.Address.Region.Name == "Washington")
					.Include(c => c.Address)
					.Include(c => c.SecondAddress);

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Two_Different_QuerableIncludes_Same_Property()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops.Where(c => c.Address.Region.Name == "Washington")
					.Select(c => c.Include(d => d.Address))
					.Select(c => c.Include(d => d.Mall))
					.Select(c => c.Include(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_Sample_Property_Twice()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops.Where(c => c.Address.Region.Name == "Washington")
					.Include(c => c.Address)
					.Include(c => c.Address);

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Implicit_Where_Join_Multiple_Depths_Not_Primary_Key()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in this.model.Shops
					where
						shop.Address.Region.Center.Label == "Center of Washington"
					select
						shop;

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Related_Object()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					from
						shop in this.model.Shops
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
					shop in this.model.Shops
					where shop.Id == this.shopId
					select shop.Include(c => c.Address.Region);

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
				Assert.AreEqual("Washington", first.Address.Region.Name);
			}
		}

		[Test]
		public void Test_Select_Two_Implicit_Joins_At_Nested_Levels()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Select(c => new { c.Address, c.Address.Region });

				var first = query.First();
			}
		}

		[Test]
		public void Test_Select_Two_Implicit_Joins_At_Same_Level()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
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
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street" && c.SecondAddress.Street == "Jefferson Avenue")
					.Select(c => c.Include(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsTrue(first.SecondAddress.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Implicit_Join_On_RelatedObject_And_Other_Related_Object_Of_Same_Type2()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street" )
					.Where(c => c.SecondAddress.Street == "Jefferson Avenue")
					.Select(c => c.Include(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsTrue(first.SecondAddress.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Implicit_Join_On_RelatedObject_And_Other_Related_Object_Of_Same_Type3()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Where(c => c.SecondAddress.Street == "Jefferson Avenue")
					.Select(c => c.Include(d => d.Address).Include(d => d.SecondAddress));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
				Assert.AreEqual("Jefferson Avenue", first.SecondAddress.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => c.Include(d => d.Address).Include(d => d.Mall));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Mall.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_Off_Join()
		{
			using (var scope = new TransactionScope())
			{
				var query = (from  mall  in this.model.Malls
							join shop in this.model.Shops on mall equals shop.Mall
							select new {  mall, shop }).Include(c => c.shop.Address);

				var first = query.First();

				Assert.IsFalse(first.shop.IsDeflatedReference());
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_Select_Anon()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Malls.Include(c => c.Address).Select(c => new { c.Address.Number }).ToList();
			}
		}

		[Test]
		public void Test_NP1_Query()
		{
			using (var scope = new TransactionScope())
			{
				var mall = model.Malls.First();

				model.Shops.Where(c => c.Mall == mall).ToList();
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects2()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => c.Include(d => d.Address).Include(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects3()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => c.Include(d => d.Address.Include(e => e.Region)));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects_Via_Pair1()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => new Tuple<string, Shop>("hi",  c ))
					.Select(c => c.Item2.Include(e => e.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects_Via_Pair2()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => new Tuple<string, Shop>("hi", c ))
					.Select(c => c.Item2.Include(d => d.Address.Include(e => e.Region)));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject1()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => c.Include(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2a1()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shop
					}).Include(c => c.shop.Address);

				var first = query.First();

				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2a3()
		{
			var y = new
			{
				address = this.model.Addresses.GetReference(
				new
				{
					Id = 1,
					Region = this.model.Regions.GetReference(new { Id = 1, Name = "" })
				})
			};

			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
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

				Assert.IsFalse(first.x.y.shop.Address.IsDeflatedReference());
				Assert.IsFalse(first.x.y.shop.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2a2()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						x = new
						{
							shop
						}
					}).Include(c => c.x.shop.Address);

				var first = query.First();

				Assert.IsFalse(first.x.shop.Address.IsDeflatedReference());
				Assert.IsTrue(first.x.shop.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2b()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shop
					}).Select(c => c.Include(d => d.shop.Address));

				var first = query.First();

				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2c()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shop
					}).Where(c => c.shop.Address.Street == "Madison Street");

				var first = query.First();

				Assert.IsTrue(first.shop.Address.IsDeflatedReference());
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2d()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shop
					}).Select(c => new { c, c.shop, c.shop.Address });

				var first = query.First();

				Assert.IsFalse(first.c.shop.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2e()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
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
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => c).Select(c => c.Include(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject3b()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shopp = shop
					}).Select(c => c.shopp.Include(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject3c()
		{
			using (var scope = new TransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						x = new
						{
							shopp = shop
						}
					}).Select(c => c.x.shopp.Include(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include1()
		{
			using (var scope = new TransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
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
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsFalse(first.shop.Address.Region.IsDeflatedReference());
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
						shop in this.model.Shops
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
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsFalse(first.shop.Address.Region.IsDeflatedReference());
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
						shop in this.model.Shops
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
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsFalse(first.shop.Address.Region.IsDeflatedReference());
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
						shop in this.model.Shops
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
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference());
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
						shop in this.model.Shops
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
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
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
						shop in this.model.Shops
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
						shop in this.model.Shops
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
						shop in this.model.Shops
					where
						shop.Address.Region.Name == "Washington"
					select
						shop;

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
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
						shop in this.model.Shops
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

				scope.Flush(this.model);

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

		[Test, Ignore]
		public void Test_Include_Collection1()
		{
			using (var scope = new TransactionScope())
			{
				var malls = this.model
					.Malls
					.Include(c => c.SisterMall.Address).ToList();
			}
		}


		[Test, Ignore]
		public void Test_Include_Collection2()
		{
			using (var scope = new TransactionScope())
			{
				var malls = this.model
					.Malls
					.Include(c => c.SisterMall.Shops).ToList();
			}
		}
	}
}
