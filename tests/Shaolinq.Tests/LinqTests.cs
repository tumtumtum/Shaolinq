using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.DataAccessModel.Test;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class LinqTests
		: BaseTests
	{
		public LinqTests(string providerName)
			: base(providerName)
		{	
		}

		public void Foo(int? x)
		{
			
		}

		[Test]
		public void Foo()
		{
			object obj = 1;

			this.Foo((int?)obj);
		}

		[Test]
		public void Test_Create_Object_And_Related_Object_Then_Query()
		{
			Guid studentId;

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.NewDataAccessObject();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.NewDataAccessObject();

				student.Birthdate = new DateTime(1940, 11, 27);
				student.FirstName = "Bruce";
				student.LastName = "Lee";

				Assert.AreEqual(school, student.School);
				Assert.AreEqual(student.FirstName + " " + student.LastName, student.FullName);

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.ReferenceToDataAccessObject<Student>(studentId);

				Assert.IsTrue(student.IsWriteOnly);

				Assert.Catch(typeof(WriteOnlyDataAccessObjectException), () => Console.WriteLine(student.FirstName));

				var sameStudent = this.model.Students.First(c => c.Id == studentId);

				// First name is available now because loading the object inflats the existing instance

				Assert.AreSame(student, sameStudent);
				Assert.AreEqual("Bruce", student.FirstName);

				scope.Complete();
			}

			var students = model.Students.Where(c => c.FirstName == "Bruce").ToList();

			Assert.AreEqual(1, students.Count);

			var storedStudent = students.First();

			Assert.AreEqual("Bruce Lee", storedStudent.FullName);

			Assert.AreEqual(1, model.Schools.Count());
			Assert.IsNotNull(model.Schools.First(c => c.Name.IsLike("%Shaolinq%")));
			Assert.AreEqual(1, model.Schools.First(c => c.Name.IsLike("%Shaolinq%")).Id);
			Assert.AreEqual(1, model.Schools.First(c => c.Name.IsLike("%Shaolinq%")).Students.Count());

			students = model.Schools.First(c => c.Name.IsLike("%Shaolinq%")).Students.Where(c => c.FirstName == "Bruce" && c.LastName.StartsWith("L")).ToList();

			Assert.AreEqual(1, students.Count);

			storedStudent = students.First();

			Assert.AreEqual("Bruce Lee", storedStudent.FullName);
		}
	}
}
