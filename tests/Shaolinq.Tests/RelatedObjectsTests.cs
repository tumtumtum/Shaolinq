using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.DataAccessModel.Test;

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
		public void Test_Query_By_Comparing_Related_Objects()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.NewDataAccessObject();

				var student = school.Students.NewDataAccessObject();

				student.Firstname = "Chuck";

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var schools = (from s in this.model.Schools select s);

				var results = from student in this.model.Students
							  where student.School == schools.First()
							  select student;

				var resultsArray = results.ToArray();
			}
		}

		[Test]
		public virtual void Test_Query_With_Related_Object_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var students = this.model.Students.Where(c => c.School.Id > 0).ToList();

				Assert.That(students.Count, Is.GreaterThan(0));

				scope.Complete();
			}
		}

		[Test]
		//[Test, Ignore("Not supported yet")]
		public virtual void Test_Query_With_Related_Object_Property()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.NewDataAccessObject();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.NewDataAccessObject();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				
				/*var students = (from student in this.model.Students
				                join school in this.model.Schools
					                on student.School equals school
								where school.Name.StartsWith("Bruce")
				                select school).ToList();*/
				
				var students = this.model.Students.Where(c => c.School.Name.StartsWith("Bruce") && c.Firstname  == "Chuck").ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_Without_Related_Parent()
		{
			Assert.Catch<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var student = model.Students.NewDataAccessObject();

					student.Firstname = "Bruce";
					student.Lastname = "Lee";

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

				student.Firstname = "Bruce";
				student.Lastname = "Lee";

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

				student.Firstname = "Bruce";
				student.Lastname = "Lee";

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

				Assert.AreEqual(1, school.Students.Count());

				scope.Complete();
			}
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
				student.Firstname = "Bruce";
				student.Lastname = "Lee";

				Assert.AreEqual(school, student.School);
				Assert.AreEqual(student.Firstname + " " + student.Lastname, student.Fullname);

				Assert.Catch<InvalidPrimaryKeyPropertyAccessException>(() => Console.WriteLine(school.Id));

				scope.Flush(model);

				Assert.AreEqual(1, school.Id);
				Assert.AreEqual(1, student.School.Id);

				scope.Complete();
			}

			Assert.AreEqual(1, model.Students.FirstOrDefault().School.Id);

			Assert.AreEqual(1, model.Schools.FirstOrDefault().Id);

			var students = model.Students.Where(c => c.Firstname == "Bruce").ToList();

			Assert.AreEqual(1, students.Count);

			var storedStudent = students.First();

			Assert.AreEqual("Bruce Lee", storedStudent.Fullname);

			Assert.AreEqual(1, model.Schools.Count());
			Assert.IsNotNull(model.Schools.First(c => c.Name.IsLike("%Shaolinq%")));
			Assert.AreEqual(1, model.Schools.First(c => c.Name.IsLike("%Shaolinq%")).Id);
			Assert.AreEqual(1, model.Schools.First(c => c.Name.IsLike("%Shaolinq%")).Students.Count());

			students = model.Schools.First(c => c.Name.IsLike("%Shaolinq%")).Students.Where(c => c.Firstname == "Bruce" && c.Lastname.StartsWith("L")).ToList();

			Assert.AreEqual(1, students.Count);

			storedStudent = students.First();

			Assert.AreEqual("Bruce Lee", storedStudent.Fullname);
		}
	}
}
