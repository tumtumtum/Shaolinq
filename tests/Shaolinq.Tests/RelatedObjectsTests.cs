using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class RelatedObjectTests
		: BaseTests
	{
		public RelatedObjectTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Create_Object_Without_Related_Parent()
		{
			Assert.Catch<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var student = model.Students.NewDataAccessObject();

					student.FirstName = "Bruce";
					student.LastName = "Lee";

					scope.Complete();
				}
			});
		}

		[Test]
		public void Test_Create_Object_From_Related_Parent()
		{
			long schoolId; 
			Guid studentId;
			
			using (var scope = new TransactionScope())
			{
				var school = model.Schools.NewDataAccessObject();

				school.Name = "Kung Fu School 1";

				var student = school.Students.NewDataAccessObject();

				student.FirstName = "Bruce";
				student.LastName = "Lee";

				Assert.AreSame(school, student.School);

				scope.Flush(model);

				schoolId = school.Id;
				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = model.Students.First(c => c.Id == studentId);

				Assert.IsTrue(student.School.IsDeflatedReference);

				Assert.AreEqual(schoolId, student.School.Id);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_And_Set_Related_Parent()
		{
			long schoolId;
			Guid studentId;

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.NewDataAccessObject();

				school.Name = "Kung Fu School 1";

				var student = model.Students.NewDataAccessObject();

				student.FirstName = "Bruce";
				student.LastName = "Lee";

				student.School = school;

				Assert.AreSame(school, student.School);

				scope.Flush(model);

				schoolId = school.Id;
				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = model.Students.First(c => c.Id == studentId);

				Assert.IsTrue(student.School.IsDeflatedReference);

				Assert.AreEqual(schoolId, student.School.Id);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.First(c => c.Id == schoolId);

				var student = model.Students.First(c => c.School == school);

				Assert.AreEqual(studentId, student.Id);

				Assert.AreEqual(1, model.Students.Count(c => c.School == school));

				Assert.AreEqual(1, school.Students.Count());

				var anotherSchool = model.Schools.NewDataAccessObject();
				var anotherStudent = anotherSchool.Students.NewDataAccessObject();

				scope.Flush(model);

				Assert.AreEqual(2, model.Students.Count());
				Assert.AreEqual(1, school.Students.Count());

				scope.Complete();
			}
		}
	}
}
