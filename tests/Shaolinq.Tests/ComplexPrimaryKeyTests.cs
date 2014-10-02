using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
