using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	public class TestNonPrimaryAutoIncrement
		: BaseTests
	{
		public TestNonPrimaryAutoIncrement(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Create_Object_With_Non_Primary_Auto_Increment()
		{
			long dog1Id, dog2Id;

			using (var scope = new TransactionScope())
			{
				var dog1 = this.model.Dogs.Create();
				scope.Flush(this.model);
				dog1Id = dog1.Id;

				var dog2 = this.model.Dogs.Create();
				scope.Flush(this.model);
				dog2Id = dog2.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var dog1 = this.model.Dogs.FirstOrDefault(c => c.Id == dog1Id);
				var dog2 = this.model.Dogs.FirstOrDefault(c => c.Id == dog2Id);

				Assert.AreNotEqual(dog1.SerialNumber, dog2.SerialNumber);
				Assert.AreNotEqual(dog1.FavouriteGuid, dog2.FavouriteGuid);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Non_Primary_Auto_Increment_And_Explicitly_Set()
		{
			long dog1Id, dog2Id, dog3Id;

			using (var scope = new TransactionScope())
			{
				var dog1 = this.model.Dogs.Create();
				scope.Flush(this.model);
				dog1Id = dog1.Id;
				//Assert.AreNotEqual(0, dog1.SerialNumber);

				var dog2 = this.model.Dogs.Create();
				dog2.SerialNumber = 1001;
				scope.Flush(this.model);
				dog2Id = dog2.Id;
				Assert.AreNotEqual(0, dog2.SerialNumber);

				var dog3 = this.model.Dogs.Create();
				scope.Flush(this.model);
				dog3Id = dog3.Id;
				Assert.AreNotEqual(0, dog3.SerialNumber);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var dog1 = this.model.Dogs.FirstOrDefault(c => c.Id == dog1Id);
				var dog2 = this.model.Dogs.FirstOrDefault(c => c.Id == dog2Id);
				var dog3 = this.model.Dogs.FirstOrDefault(c => c.Id == dog3Id);

				Assert.AreNotEqual(dog1.SerialNumber, dog2.SerialNumber);
				Assert.AreEqual(1001, dog2.SerialNumber);
				Assert.AreNotEqual(dog1.SerialNumber, dog3.SerialNumber);

				scope.Complete();
			}
		}
	}
}
