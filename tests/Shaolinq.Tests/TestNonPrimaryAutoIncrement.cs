using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
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
			Guid student1Id, student2Id;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				var student1 = school.Students.Create();

				scope.Flush(this.model);
				student1Id = student1.Id;

				var student2 = school.Students.Create();
				scope.Flush(this.model);
				student2Id = student2.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student1 = this.model.Students.FirstOrDefault(c => c.Id == student1Id);
				var student2 = this.model.Students.FirstOrDefault(c => c.Id == student2Id);

				Assert.AreNotEqual(student1.SerialNumber, student2.SerialNumber);
				Assert.AreNotEqual(student1.RandomGuid, student2.RandomGuid);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Non_Primary_Auto_Increment_And_Explicitly_Set()
		{
			Guid student1Id, student2Id, student3Id;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				var student1 = school.Students.Create();
				scope.Flush(this.model);
				student1Id = student1.Id;
				
				var student2 = school.Students.Create();
				student2.SerialNumber = 1001;
				scope.Flush(this.model);
				student2Id = student2.Id;
				Assert.AreNotEqual(0, student2.SerialNumber);

				var student3 = school.Students.Create();
				scope.Flush(this.model);
				student3Id = student3.Id;
				Assert.AreNotEqual(0, student3.SerialNumber);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student1 = this.model.Students.FirstOrDefault(c => c.Id == student1Id);
				var student2 = this.model.Students.FirstOrDefault(c => c.Id == student2Id);
				var student3 = this.model.Students.FirstOrDefault(c => c.Id == student3Id);

				Assert.AreNotEqual(student1.SerialNumber, student2.SerialNumber);
				Assert.AreEqual(1001, student2.SerialNumber);
				Assert.AreNotEqual(student1.SerialNumber, student3.SerialNumber);

				scope.Complete();
			}
		}
	}
}
