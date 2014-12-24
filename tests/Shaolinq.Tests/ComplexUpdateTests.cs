using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.ComplexPrimaryKeyModel;

namespace Shaolinq.Tests
{
	///[TestFixture("MySql")]
	[TestFixture("Sqlite")]
	//[TestFixture("SqliteInMemory")]
	//[TestFixture("SqliteClassicInMemory")]
	//[TestFixture("Postgres")]
	//[TestFixture("Postgres.DotConnect")]
	public class ComplexUpdateTests
		: BaseTests<ComplexPrimaryKeyDataAccessModel>
	{
		public ComplexUpdateTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Set_Object_Property_To_Null()
		{
			long addressId;
			long regionId;

			using (var scope = new TransactionScope())
			{
				var address = this.model.Addresses.Create();

				address.Region = this.model.Regions.Create();
				address.Region.Name = "RegionName";

				this.model.Flush();

				addressId = address.Id;
				regionId = address.Region.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var address = this.model.Addresses.GetByPrimaryKey(this.model.Addresses.GetReference(new { Id = addressId, Region = this.model.Regions.GetReference(new { Id = regionId, Name = "RegionName"})}));

				address.Region = null;

				var changedProperties = address.GetChangedProperties();
				var changedPropertiesFlattened = address.GetAdvanced().GetChangedPropertiesFlattened();

				Assert.AreEqual(1, changedProperties.Count);
				Assert.AreEqual(1, changedPropertiesFlattened.Count);

				scope.Complete();
			}
		}

		[Test, ExpectedException(typeof(MissingOrInvalidPrimaryKeyException))]
		public void Test_Create_Object_With_Incomplete_Complex_Primary_Key()
		{
			try
			{
				using (var scope = new TransactionScope())
				{
					var address = this.model.Addresses.Create();
					
					address.Region = this.model.Regions.Create();
					address.Region2 = address.Region;
					address.Region2 = null;

					var changedProperties = address.GetChangedProperties();
					var changedPropertiesFlattened = address.GetAdvanced().GetChangedPropertiesFlattened();
					
					Assert.IsTrue(address.GetAdvanced().IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys);
					Assert.IsFalse(address.GetAdvanced().PrimaryKeyIsCommitReady);
					Assert.AreEqual(changedProperties.Count, address.GetAllProperties().Length);
					
					scope.Complete();
				}
			}
			catch (TransactionAbortedException e)
			{
				throw e.InnerException;
			}
		}

		[Test]
		public void Test_Create_Incomplete_Objects_Then_Delete()
		{
			using (var scope = new TransactionScope())
			{
				var shop = this.model.Shops.Create();
				var address = this.model.Addresses.Create();
				var region = this.model.Regions.Create();

				address.Delete();
				shop.Delete();
				region.Delete();
				
				scope.Complete();
			}
		}
	}
}
