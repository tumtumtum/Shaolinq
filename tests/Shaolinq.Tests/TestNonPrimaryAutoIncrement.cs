// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	public class TestNonPrimaryAutoIncrement
		: BaseTests<TestDataAccessModel>
	{
		public TestNonPrimaryAutoIncrement(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Create_Object_With_Non_Primary_Auto_Increment()
		{
			Guid object1Id, object2Id;

			using (var scope = new TransactionScope())
			{
				var object1 = this.model.NonPrimaryAutoIncrementObjectWithManyTypes.Create();

				scope.Flush(this.model);
				object1Id = object1.Id;

				var object2 = this.model.NonPrimaryAutoIncrementObjectWithManyTypes.Create();
				scope.Flush(this.model);
				object2Id = object2.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var object1 = this.model.NonPrimaryAutoIncrementObjectWithManyTypes.FirstOrDefault(c => c.Id == object1Id);
				var object2 = this.model.NonPrimaryAutoIncrementObjectWithManyTypes.FirstOrDefault(c => c.Id == object2Id);

				Assert.AreNotEqual(object1.SerialNumber, object2.SerialNumber);
				Assert.AreNotEqual(object1.RandomGuid, object2.RandomGuid);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Non_Primary_Auto_Increment_And_Explicitly_Set()
		{
			Guid object1Id, object2Id, object3Id;
			
			using (var scope = new TransactionScope())
			{
				var object1 = this.model.NonPrimaryAutoIncrementObjectWithManyTypes.Create();
				scope.Flush(this.model);
				object1Id = object1.Id;

				var object2 = this.model.NonPrimaryAutoIncrementObjectWithManyTypes.Create();
				object2.SerialNumber = 1001;
				scope.Flush(this.model);
				object2Id = object2.Id;
				Assert.AreNotEqual(0, object2.SerialNumber);
				
				var object3 = this.model.NonPrimaryAutoIncrementObjectWithManyTypes.Create();
				object3.SerialNumber = 1002;
				scope.Flush(this.model);
				object3Id = object3.Id;
				Assert.AreNotEqual(0, object3.SerialNumber);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var object1 = this.model.NonPrimaryAutoIncrementObjectWithManyTypes.FirstOrDefault(c => c.Id == object1Id);
				var object2 = this.model.NonPrimaryAutoIncrementObjectWithManyTypes.FirstOrDefault(c => c.Id == object2Id);
				var object3 = this.model.NonPrimaryAutoIncrementObjectWithManyTypes.FirstOrDefault(c => c.Id == object3Id);

				Assert.AreNotEqual(object1.SerialNumber, object2.SerialNumber);
				Assert.AreEqual(1001, object2.SerialNumber);
				Assert.AreNotEqual(object1.SerialNumber, object3.SerialNumber);

				scope.Complete();
			}
		}
	}
}
