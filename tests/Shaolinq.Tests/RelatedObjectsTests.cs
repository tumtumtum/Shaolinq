// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
	[TestFixture("SqlServer", Category = "IgnoreOnMono")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	public class RelatedObjectTests
		: BaseTests<TestDataAccessModel>
	{
		public RelatedObjectTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Self_Referencing_One_To_Many_Relationship()
		{
			long marsId;
			long parentCatId;

			using (var scope = new TransactionScope())
			{
				var cat = this.model.Cats.Create();
				var parentCat = this.model.Cats.Create();

				cat.Name = "Mars";
				cat.Parent = parentCat;

				var neighbourhoodCat = parentCat.Kittens.Create();
				neighbourhoodCat.Name = "Fluffy";

				scope.Flush();

				marsId = cat.Id;
				parentCatId = parentCat.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var cat = this.model.Cats.FirstOrDefault(c => c.Id == marsId);
				
				Assert.AreEqual("Mars", cat.Name);
				Assert.AreEqual(parentCatId, cat.Parent.Id);

				var parentCat = cat.Parent;

				Assert.AreEqual(2, parentCat.Kittens.Count());

				var kittens =  parentCat.Kittens.OrderBy(c => c.Name).Select(c => c.Name).ToList();

				Assert.That(kittens, Is.EquivalentTo(new [] {"Fluffy", "Mars"}));
				
				scope.Complete();
			}
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
				var firstSchool = (from s in this.model.Schools select s).First();

				var results = from student in this.model.Students
							  where student.School == firstSchool
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

				scope.Flush();

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

				scope.Flush();

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

				scope.Flush();

				var schoolsAndAddresses = this.model.Students
					.Select(c => new { c.School, c.Address}).ToList();

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Select_Related_Object_Implict_Join_3()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush();

				//var addresses = this.model.Students
				//.Select(c => c.School.Name == "Bruce's Kung Fu School" ? c.Address : c.Address).ToList();

				var addresses = this.model.Students
					.Select(c => new { c.School, c.Address }).ToList();
				//.Select(c => c.School.Name == "" ? c.Address : c.Address).ToList();

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

				scope.Flush();
				
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

				scope.Flush();

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

				scope.Flush();

				var values = this.model.Students.Select(c => c.School == brucesSchool  ? true : false).ToList();

				Assert.That(values.Count, Is.GreaterThan(0));
				
				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_Select_Related_Object_Select_With_Anonymous_Projecton()
		{
			using (var scope = new TransactionScope())
			{
				var brucesSchool = this.model.Schools.Create();

				brucesSchool.Name = "Bruce's Kung Fu School";

				var brucesStudent = brucesSchool.Students.Create();

				brucesStudent.Firstname = "Chuck";

				scope.Flush();

				var values = this.model.Students.Select(c => new { Nickname = c.Nickname.Substring(1) + "A", c.Sex }).ToList();
			
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

				scope.Flush();

				var values = this.model.Students.Select(c => c.School == null ? true : false).ToList();

				Assert.That(values.Count, Is.GreaterThan(0));

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

				scope.Flush();

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

				scope.Flush();

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

				scope.Flush();

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

				scope.Flush();

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

				scope.Flush();

				var students = this.model.Students.Where(c => c == brucesStudent).ToList();

				Assert.AreEqual(1, students.Count);
				Assert.AreSame(brucesStudent, students[0]);

				scope.Complete();
			}
		}

		[Test, ExpectedException(typeof(MissingPropertyValueException))]
		public void Test_Create_Object_Without_Related_Parent()
		{
			try
			{
				using (var scope = new TransactionScope())
				{
					var student = this.model.Students.Create();

					student.Firstname = "Bruce";
					student.Lastname = "Lee";

					scope.Complete();
				}
			}
			catch (TransactionAbortedException e)
			{
				throw e.InnerException;
			}
		}

		[Test]
		public void Test_Create_Object_From_Related_Parent()
		{
			long schoolId; 
			Guid studentId;
			
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "Kung Fu School 1";

				var student = school.Students.Create();

				student.Firstname = "Bruce";
				student.Lastname = "Lee";

				Assert.AreSame(school, student.School);

				scope.Flush();

				schoolId = school.Id;
				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.First(c => c.Id == studentId);

				Assert.IsTrue(student.School.IsDeflatedReference());

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
				var school = this.model.Schools.Create();

				school.Name = "Kung Fu School 1";

				var student = this.model.Students.Create();

				student.Firstname = "Bruce";
				student.Lastname = "Lee";

				student.School = school;

				Assert.AreSame(school, student.School);

				scope.Flush();

				schoolId = school.Id;
				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.First(c => c.Id == studentId);

				Assert.IsTrue(student.School.IsDeflatedReference());

				Assert.AreEqual(schoolId, student.School.Id);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.First(c => c.Id == schoolId);

				var student = this.model.Students.First(c => c.School == school);

				Assert.AreEqual(studentId, student.Id);

				Assert.AreEqual(1, this.model.Students.Count(c => c.School == school));

				Assert.AreEqual(1, school.Students.Count());

				var anotherSchool = this.model.Schools.Create();
				var anotherStudent = anotherSchool.Students.Create();

				scope.Flush();

				Assert.AreEqual(1, school.Students.Count());

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_And_Related_Object_Then_Query()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

				student.Birthdate = new DateTime(1940, 11, 27);
				student.Firstname = "Bruce";
				student.Lastname = "Lee";

				Assert.AreEqual(school, student.School);
				Assert.AreEqual(student.Firstname + " " + student.Lastname, student.Fullname);

				Assert.Catch<InvalidPrimaryKeyPropertyAccessException>(() => Console.WriteLine(school.Id));

				scope.Flush();

				Assert.AreEqual(1, school.Id);
				Assert.AreEqual(1, student.School.Id);

				scope.Complete();
			}

			Assert.AreEqual(1, this.model.Students.FirstOrDefault().School.Id);

			Assert.AreEqual(1, this.model.Schools.FirstOrDefault().Id);

			var students = this.model.Students.Where(c => c.Firstname == "Bruce").ToList();

			Assert.AreEqual(1, students.Count);

			var storedStudent = students.First();

			Assert.AreEqual("Bruce Lee", storedStudent.Fullname);

			Assert.AreEqual(1, this.model.Schools.Count());
			Assert.IsNotNull(this.model.Schools.First(c => c.Name.IsLike("%Shaolinq%")));
			Assert.AreEqual(1, this.model.Schools.First(c => c.Name.IsLike("%Shaolinq%")).Id);
			Assert.AreEqual(1, this.model.Schools.First(c => c.Name.IsLike("%Shaolinq%")).Students.Count());

			students = this.model.Schools.First(c => c.Name.IsLike("%Shaolinq%")).Students.Where(c => c.Firstname == "Bruce" && c.Lastname.StartsWith("L")).ToList();

			Assert.AreEqual(1, students.Count);

			storedStudent = students.First();

			Assert.AreEqual("Bruce Lee", storedStudent.Fullname);
		}
	}
}
