// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
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
				var school = this.model.Schools.Create();

				var student = school.Students.Create();

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
				var school = this.model.Schools.Create();

				var student = school.Students.Create();

				student.Firstname = "TQWROP";

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var students = this.model.Students.Where(c => c.School.Id > 0).ToList();

				Assert.That(students.Count, Is.GreaterThan(0));

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Select_And_Project_Related_Object_Property()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var names = this.model.Students.Select(c => c.Firstname + "jo").ToList();

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Select_Related_Object_Implict_Join_1()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var students = this.model.Students.Where(c => c.Firstname == "Chuck").Select(c => c.School).ToList();

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Select_Related_Object_Implict_Join_2()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var addresses = this.model.Students.Select(c => c.School.Name == "Bruce's Kung Fu School" ? c.Address : c.Address).ToList();

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Where_Related_Object_Compared_To_Null()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);
				
				var schools = this.model.Students.Where(c => c.School == null).ToList();

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Where_Related_Object_Compared_To_Variable()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var students = this.model.Students.Where(c => c.School == brucesSchool).ToList();

				Assert.AreEqual(1, students.Count);
				Assert.AreSame(brucesSchool, students[0].School);

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Select_Related_Object_Select_With_Conditional()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var values = this.model.Students.Select(c => c.School == brucesSchool  ? true : false).ToList();

				Assert.That(values.Count, Is.GreaterThan(0));
				
				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Select_Related_Object_Select_With_Conditional_On_Null()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var values = this.model.Students.Select(c => c.School == null ? true : false).ToList();

				Assert.That(values.Count, Is.GreaterThan(0));

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Select_Related_Object_Select_With_Conditional_On_Implicit_Join_Value()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var students = this.model.Students.Select(c => c.Address.Number == 0 ? c.School : brucesSchool).ToList();

				Assert.That(students.Count, Is.GreaterThan(0));
				
				scope.Complete();
			}
		}


		[Test]
		public virtual void Test_Query_Where_Related_Object_Property_Compared_To_Variable()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Address = this.model.Address.Create();
				brucesStudent.Address.Number = 1087;

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var id = brucesStudent.Address.Number;
				var students = this.model.Students.Where(c => c.Address.Number == id).ToList();

				Assert.AreEqual(1, students.Count);
				Assert.AreSame(brucesStudent, students[0]);

				students = this.model.Students.Where(c => c.Address.Number != id).ToList();

				Assert.That(students.Count, Is.LessThan(this.model.Students.Count()));
				Assert.IsFalse(students.Contains(brucesStudent));

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Where_Related_Object_Property_Compared_To_Variable_Complex()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Address = this.model.Address.Create();
				brucesStudent.Address.Number = 1088;

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var students = this.model.Students.Where(c => c.Address.Number == brucesStudent.Address.Number).ToList();

				Assert.AreEqual(1, students.Count);
				Assert.AreSame(brucesStudent, students[0]);

				students = this.model.Students.Where(c => c.Address.Number != brucesStudent.Address.Number).ToList();

				Assert.That(students.Count, Is.LessThan(this.model.Students.Count()));
				Assert.IsFalse(students.Contains(brucesStudent));

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Select_Related_Object_Property_Implicit_Join()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var schoolName = this.model.Students.Where(c => c == brucesStudent).Select(c => c.School.Name).First();

				Assert.AreEqual(brucesSchool.Name, schoolName);

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Where_With_Multiple_Related_Object_Property_Multiple_Implicit_Joins()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var students = this.model.Students.Where(c => c.School.Name.StartsWith("Bruce") && c.Address.Number == 0).ToList();

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Where_Compare_Object_To_Null()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush(model);

				var students = this.model.Students.Where(c => c == brucesStudent).ToList();

				Assert.AreEqual(1, students.Count);
				Assert.AreSame(brucesStudent, students[0]);

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
					var student = model.Students.Create();

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
				var school = model.Schools.Create();

				school.Name = "Kung Fu School 1";

				var student = school.Students.Create();

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
				var school = model.Schools.Create();

				school.Name = "Kung Fu School 1";

				var student = model.Students.Create();

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

				var anotherSchool = model.Schools.Create();
				var anotherStudent = anotherSchool.Students.Create();

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
				var school = model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

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
