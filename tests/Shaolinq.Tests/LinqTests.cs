using System;
using System.Collections.Generic;
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

		[Test]
		public void Test_Create_Object_And_Related_Object_Then_Query()
		{
			using (var scope = new TransactionScope())
			{
				var school = model.Schools.NewDataAccessObject();

				Console.Write(model.GetPersistenceContext(school.GetType()).PersistenceStoreName);

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.NewDataAccessObject();

				student.Birthdate = new DateTime(1940, 11, 27);
				student.FirstName = "Bruce";
				student.LastName = "Lee";

				Assert.AreEqual(school, student.School);
				Assert.AreEqual(student.FirstName + " " + student.LastName, student.FullName);

				Assert.Catch<InvalidPrimaryKeyPropertyAccessException>(() => Console.WriteLine(school.Id));

				scope.Flush(model);

				Assert.AreEqual(1, school.Id);
				Assert.AreEqual(1, student.School.Id);

				scope.Complete();
			}

			Assert.AreEqual(1, model.Students.FirstOrDefault().School.Id);

			Assert.AreEqual(1, model.Schools.FirstOrDefault().Id);

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
