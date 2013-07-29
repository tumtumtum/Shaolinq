using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class TransactionTests
		: BaseTests
	{
		public TransactionTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Create_Object()
		{
			using (var scope = new TransactionScope())
			{
				var school = model.Schools.NewDataAccessObject();
				
				school.Name = "Kung Fu School";

				var student = model.Students.NewDataAccessObject();

				student.FirstName = "Bruce";
				student.LastName = "Lee";
				student.School = school;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = model.Students.First(c => c.FirstName == "Bruce");

				Assert.AreEqual("Bruce Lee", student.FullName);
			}
		}

		[Test]
		public void Test_Create_Object_And_Abort()
		{
			using (var scope = new TransactionScope())
			{
				var student = model.Students.NewDataAccessObject();

				student.FirstName = "StudentThatShouldNotExist";
			}

			using (var scope = new TransactionScope())
			{
				Assert.Catch<InvalidOperationException>(() => model.Students.First(c => c.FirstName == "StudentThatShouldNotExist"));
			}
		}
	}
}
