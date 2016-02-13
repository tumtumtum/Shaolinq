// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.ComplexPrimaryKeyModel;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	[TestFixture("Postgres")]
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
			long regionId;
			long addressId;
			
			using (var scope = new TransactionScope())
			{
				var address = this.model.Addresses.Create();

				address.Region = this.model.Regions.Create();
				address.Region.Name = "RegionName";
				address.Region2 = this.model.Regions.Create();
				address.Region2.Name = "RegionName2";

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
				Assert.AreEqual(this.model.TypeDescriptorProvider.GetTypeDescriptor(typeof(Region)).PrimaryKeyCount, changedPropertiesFlattened.Count);
			}

			using (var scope = new TransactionScope())
			{
				var address = this.model.Addresses.GetByPrimaryKey(this.model.Addresses.GetReference(new { Id = addressId, Region = this.model.Regions.GetReference(new { Id = regionId, Name = "RegionName" }) }));

				Assert.IsNotNull(address.Region2);
				address.Region2 = null;

				var changedProperties = address.GetChangedProperties();
				var changedPropertiesFlattened = address.GetAdvanced().GetChangedPropertiesFlattened();

				Assert.AreEqual(1, changedProperties.Count);
				Assert.AreEqual(this.model.TypeDescriptorProvider.GetTypeDescriptor(typeof(Region)).PrimaryKeyCount, changedPropertiesFlattened.Count);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var address = this.model.Addresses.GetByPrimaryKey(this.model.Addresses.GetReference(new { Id = addressId, Region = this.model.Regions.GetReference(new { Id = regionId, Name = "RegionName" }) }));

				Assert.IsNull(address.Region2);
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
					var changedPropesrtiesFlattened = address.GetAdvanced().GetChangedPropertiesFlattened();
					
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

		[Test]
		public void Test_Nested_Scope_Update()
		{
			var e = new ManualResetEvent(false);

			var task = Test_Nested_Scope_Update_Async(e).ConfigureAwait(false);

			if (task.GetAwaiter().IsCompleted)
			{
				return;
			}

			e.WaitOne(TimeSpan.FromSeconds(10));

			Assert.IsTrue(task.GetAwaiter().IsCompleted);
		}

		private async Task Test_Nested_Scope_Update_Async(ManualResetEvent e)
		{
			Guid id;
			var methodName = MethodBase.GetCurrentMethod().Name;

			using (var scope = new DataAccessScope())
			{
				var child = this.model.Children.Create();

				await scope.FlushAsync().ConfigureAwait(false);

				id = child.Id;

				using (var inner = new DataAccessScope())
				{
					child.Nickname = methodName;

					inner.Complete();
				}

				await scope.FlushAsync().ConfigureAwait(false);

				Assert.AreEqual(child.Id, this.model.Children.Single(c => c.Nickname == methodName).Id);

				scope.Complete();
			}
			
			Assert.AreEqual(id, this.model.Children.Single(c => c.Nickname == methodName).Id);

			e.Set();
		}
		
		[Test, ExpectedException(typeof(DataAccessTransactionAbortedException))]
		public void Test_Nested_Scope_Abort()
		{
			var methodName = MethodBase.GetCurrentMethod().Name;

			using (var scope = new DataAccessScope())
			{
				var child = this.model.Children.Create();

				scope.Flush();

				using (var inner = new DataAccessScope())
				{
					child.Nickname = methodName;
				}

				scope.Flush();

				Assert.AreEqual(child.Id, this.model.Children.Single(c => c.Nickname == methodName).Id);

				scope.Complete();
			}
		}
	}
}
