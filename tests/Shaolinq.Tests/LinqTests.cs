// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Persistence.Linq;
using Shaolinq.Tests.TestModel;
using Shouldly;

namespace Shaolinq.Tests
{
	[TestFixture("MySql:DataAccessScope")]
	[TestFixture("Postgres:DataAccessScope")]
	[TestFixture("Postgres.DotConnect:DataAccessScope")]
	[TestFixture("Postgres.DotConnect.Unprepared:DataAccessScope")]
	[TestFixture("Sqlite")]
	[TestFixture("Sqlite:DataAccessScope")]
	[TestFixture("SqliteInMemory:DataAccessScope")]
	[TestFixture("SqliteClassicInMemory:DataAccessScope")]
	[TestFixture("SqlServer:DataAccessScope")]
	public class LinqTests
		: BaseTests<TestDataAccessModel>
	{
		public LinqTests(string providerName)
			: base(providerName)
		{
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			CreateObjects();	
		}

		private void CreateObjects()
		{
			using (var scope = NewTransactionScope())
			{
				var schoolWithNoStudents = this.model.Schools.Create();

				schoolWithNoStudents.Name = "Empty school";

				var school = this.model.Schools.Create();

				school.Name = "Bruce's Kung Fu School";
				school.Address = this.model.Address.Create();

				scope.Flush();

				var tum = school.Students.Create();

				scope.Flush();

				var count = school.Students.Count();

				var address = this.model.Address.Create();
				address.Number = 178;
				address.Street = "Fake Street";

				tum.Firstname = "Tum";
				tum.Lastname = "Nguyen";
				tum.Sex = Sex.Male;
				tum.Address = address;
				tum.Height = 177;
				tum.FavouriteNumber = 36;
				tum.Address = address;
				tum.TimeSinceLastSlept = TimeSpan.FromHours(7.5);
				tum.SpecialDate = new DateTime(1979, 12, 24, 04, 00, 00);
				tum.Birthdate = new FixedDate(new DateTime(1979, 12, 24, 00, 00, 00, DateTimeKind.Utc));
				tum.FavouriteDate = new FixedDate(new DateTime(2016, 08, 19, 00, 00, 00, DateTimeKind.Utc));

				var mars = school.Students.Create();

				mars.Firstname = "Mars";
				mars.Lastname = "Nguyen";
				mars.Nickname = "The Cat";
				mars.Height = 20;
				mars.Address = address;
				mars.Sex = Sex.Female;
				mars.BestFriend = tum;
				mars.SpecialDate = new DateTime(2003, 11, 2);
				mars.FavouriteNumber = 1;
				mars.Birthdate = null;

				var cat1 = tum.Cats.Create();
				var cat2 = tum.Cats.Create();

				cat1.Name = "Cat1";
				cat2.Name = "Cat2";

				school = this.model.Schools.Create();

				school.Name = "Brandon's Kung Fu School";

				var chuck1 = school.Students.Create();
				
				scope.Flush();
				
				var address2 = this.model.Address.Create();
				address2.Number = 1799;
				address2.Street = "Fake Street";

				chuck1.Firstname = "Chuck";
				chuck1.Lastname = "Norris";
				chuck1.Nickname = "God";
				chuck1.Address = address2;
				chuck1.Height = 10000;
				chuck1.FavouriteNumber = 8;
				chuck1.Weight = 1000;

				var chuck2 = school.Students.Create();

				chuck2.Firstname = "Chuck";
				chuck2.Lastname = "Yeager";
				chuck2.Height = 182;
				chuck2.Weight = 70;
				chuck2.Sex = Sex.Male;

				var marsCat = this.model.Cats.Create();
				marsCat.Name = "Mars";

				var bird = this.model.Birds.Create(marsCat);

				scope.Flush();
				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				var apple = this.model.Apples.Create();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Schema()
		{
			var expressions = this.model
				.GetCurrentSqlDatabaseContext()
				.SchemaManager
				.BuildDataDefinitonExpressions(DatabaseCreationOptions.DeleteExistingDatabase);

			var s = this.model
				.GetCurrentSqlDatabaseContext()
				.SqlQueryFormatterManager
				.Format(expressions, SqlQueryFormatterOptions.EvaluateConstantPlaceholders | SqlQueryFormatterOptions.EvaluateConstants);

			if (this.ProviderName.Contains("SqlServer"))
			{
				s.CommandText.ShouldContain("CREATE UNIQUE INDEX \"idx_student_id_sex_sexoptional\" ON \"Student\"(\"StudentId\", \"Sex\", \"SexOptional\") WHERE ((((\"Sex\") = (\'Male\'))) AND ((\"SexOptional\" IS NOT NULL)));");
				s.CommandText.ShouldContain("CREATE UNIQUE INDEX \"idx_student_id_overseas_sexoptional\" ON \"Student\"(\"StudentId\", \"Overseas\", \"SexOptional\") WHERE ((((\"SexOptional\") = (\'Male\'))) AND ((\"SexOptional\" IS NOT NULL)));");
			}
		}

		[Test]
		public void TestGenericDao()
		{
			using (var scope = NewTransactionScope())
			{
				var dao = this.model.ConcreteGenericDaos.Create();

				dao.RelatedObject = this.model.Schools.First();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Get_CustomPrimaryKeyObject()
		{
			using (var scope = NewTransactionScope())
			{
				var mars = this.model.Cats.Single(c => c.Name == "Mars");

				Assert.IsNotNull(this.model.Birds.Single(c => c.Owner == mars));

				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				var mars = this.model.Cats.Single(c => c.Name == "Mars");

				Assert.IsNotNull(this.model.Birds.Single(c => c.Owner.Id == mars.Id));

				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				var mars = this.model.Cats.Single(c => c.Name == "Mars");

				Assert.IsNotNull(this.model.Birds.Single(c => c.Owner.Name == mars.Name));

				scope.Complete();
			}
		}
		
		[Test]
		public void Test_Get_Timespan()
		{
			using (var scope = NewTransactionScope())
			{
				var student = this.model.Students.First(c => c.Firstname == "Tum");

				Assert.IsNotNull(student);
				Assert.AreEqual(TimeSpan.FromHours(7.5), student.TimeSinceLastSlept);

				student.TimeSinceLastSlept = TimeSpan.FromMilliseconds(2);

				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				var student = this.model.Students.First(c => c.Firstname == "Tum");

				Assert.IsNotNull(student);
				Assert.AreEqual(TimeSpan.FromMilliseconds(2), student.TimeSinceLastSlept);
			}

			using (var scope = NewTransactionScope())
			{
				var student = this.model.Students.First(c => c.Firstname == "Tum");

				student.TimeSinceLastSlept = TimeSpan.FromMilliseconds(2015);

				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				var student = this.model.Students.First(c => c.Firstname == "Tum");

				Assert.IsNotNull(student);
				Assert.AreEqual(TimeSpan.FromMilliseconds(2015), student.TimeSinceLastSlept);
			}

			using (var scope = NewTransactionScope())
			{
				var student = this.model.Students.First(c => c.Firstname == "Tum");

				student.TimeSinceLastSlept = TimeSpan.FromMilliseconds(999);

				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				var student = this.model.Students.First(c => c.Firstname == "Tum");

				Assert.IsNotNull(student);
				Assert.AreEqual(TimeSpan.FromSeconds(0.999), student.TimeSinceLastSlept);
			}
		}

		[Test, Ignore("NotSupported yet")]
		public void Test_Select_Many_With_Non_Table()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.OrderBy(c => c.Id).SelectMany(c => this.model.Schools.OrderBy(d => d.Id), (x, y) => new
				{
					x,
					y
				}).ToList();

				var result2 = (from x in this.model.Students.OrderBy(c => c.Id)
							   from y in this.model.Schools.OrderBy(c => c.Id)
							   select new
							   {
								   x,
								   y
							   }).ToList();
			}
		}

		[Test]
		public void Test_Select_Many()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.SelectMany(c => this.model.Schools, (x, y) => new
				{
					x,
					y
				}).ToList();
				var result2 = (from x in this.model.Students
							   from y in this.model.Schools
							   select new
							   {
								   x,
								   y
							   }).ToList();


				var result3 = (from x in this.model.Students.ToList()
							   from y in this.model.Schools.ToList()
							   select new
							   {
								   x,
								   y
							   }).ToList();

				Assert.Greater(result.Count(), 0);

				Assert.IsTrue(result.Select(c => c.x).SequenceEqual(result2.Select(c => c.x)));
				Assert.IsTrue(result.Select(c => c.y).SequenceEqual(result2.Select(c => c.y)));

				Assert.IsTrue(result.Select(c => c.x).OrderBy(c => c.Id).SequenceEqual(result3.Select(c => c.x).OrderBy(c => c.Id)));
				Assert.IsTrue(result.Select(c => c.y).OrderBy(c => c.Id).SequenceEqual(result3.Select(c => c.y).OrderBy(c => c.Id)));


				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_Select_DefaultIfEmpty_Sum()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Select(c => c.Weight).DefaultIfEmpty(777).Sum();

				Assert.AreEqual(1070, result);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_With_Nested_Select_Scalar_Comparison_In_Predicate1()
		{
			using (var scope = NewTransactionScope())
			{
				var results = (from student in this.model.Students
							   where student.School.Id == ((from s in this.model.Schools where s.Name == "Bruce's Kung Fu School" select s.Id).First())
							   select student).ToList();

				Assert.That(results.Count, Is.GreaterThan(0));

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_With_Nested_Select_Scalar_Comparison_In_Predicate()
		{
			using (var scope = NewTransactionScope())
			{
				var results = (from student in this.model.Students
							   where student.School.Id == ((from s in this.model.Schools where s.Name == "Bruce's Kung Fu School" select s.Id).FirstOrDefault())
							   select student).ToList();

				Assert.That(results.Count, Is.GreaterThan(0));

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_With_Nested_Select_Scalar_Inequality_Comparison_In_Predicate()
		{
			using (var scope = NewTransactionScope())
			{
				var results = (from student in this.model.Students
							   where student.Address.Number >= ((from address in this.model.Address select address.Number).Max())
							   select student).ToList();

				Assert.That(results.Count, Is.GreaterThan(0));
			}
		}

		[Test]
		public void Test_Query_With_Nested_Select_In_Projection()
		{
			using (var scope = NewTransactionScope())
			{
				var results = (from student in this.model.Students
							   select new { Student = student, MaxAddress = (from s in this.model.Address select s.Number).Max()}).ToList();

				Assert.That(results.Count, Is.GreaterThan(0));

				var results2 = (from student in this.model.Students
							   select new { Student = student, MaxAddress = (from s in this.model.Address select s.Number).Max() }).ToList();

				results.Sort((x, y) => x.Student.Id.GetHashCode() - y.Student.Id.GetHashCode());
				results2.Sort((x, y) => x.Student.Id.GetHashCode() - y.Student.Id.GetHashCode());

				Assert.True(results.SequenceEqual(results2));
			}
		}

		[Test]
		public void Test_Query_With_Nested_Select_In_Projection_DAS()
		{
			using (var scope = new DataAccessScope())
			{
				var results = (from student in this.model.Students
							   select new { Student = student, MaxAddress = (from s in this.model.Address select s.Number).Max() }).ToList();

				Assert.That(results.Count, Is.GreaterThan(0));

				var results2 = (from student in this.model.Students
								select new { Student = student, MaxAddress = (from s in this.model.Address select s.Number).Max() }).ToList();

				results.Sort((x, y) => x.Student.Id.GetHashCode() - y.Student.Id.GetHashCode());
				results2.Sort((x, y) => x.Student.Id.GetHashCode() - y.Student.Id.GetHashCode());

				Assert.True(results.SequenceEqual(results2));
			}
		}

		[Test]
		public void Test_Query_With_Nested_Select_Object_Comparison()
		{
			using (var scope = NewTransactionScope())
			{
				var results = (from student in this.model.Students
							   where student.School == (from s in this.model.Schools where s.Name == "Bruce's Kung Fu School" select s).First()
							   select student).ToList();

				Assert.That(results.Count, Is.GreaterThan(0));
			}
		}

		[Test]
		public virtual void Test_Query_GroupBy_With_Count()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
							  group student by student.Firstname into g
							  orderby g.Key
							  select new { Count = g.Count(), Name = g.Key };

				var results2 = from student in this.model.Students.ToList()
							  group student by student.Firstname into g
							  orderby g.Key
							  select new { Count = g.Count(), Name = g.Key };

				var resultsArray = results.ToArray();

				Assert.IsTrue(resultsArray.SequenceEqual(results2));
				
				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_GroupBy_And_OrderBy_Key()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
					group student by student.Firstname
					into g
					orderby g.Key
					select new {Count = g.Count(), Name = g.Key};

				var results2 = from student in this.model.Students.ToList()
							  group student by student.Firstname
					into g
							  orderby g.Key
							  select new { Count = g.Count(), Name = g.Key };

				var resultsArray = results.ToArray();

				Assert.IsTrue(resultsArray.SequenceEqual(results2));

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_GroupBy_Complex_And_OrderBy_Key()
		{
			using (var scope = NewTransactionScope())
			{
				var results1 = from student in this.model.Students
							  group student by new { student.Firstname, student.SpecialDate}
								  into g
								  select new { Count = g.Count(), g.Key, Name = g.Key.Firstname };


				var results2 = from student in this.model.Students.ToList()
							  group student by new { student.Firstname, student.SpecialDate }
												  into g
							  select new { Count = g.Count(), g.Key, Name = g.Key.Firstname };

				var resultsArray1 = results1.OrderBy(c => c.Key.Firstname).ToArray();
				var resultsArray2 = results2.OrderBy(c => c.Key.Firstname).ToArray();

				Assert.IsTrue(resultsArray1.SequenceEqual(resultsArray2));

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_GroupBy_Tuple_And_OrderBy_Key()
		{
			using (var scope = NewTransactionScope())
			{
				var results1 = from student in this.model.Students
							  group student by new Tuple<string, DateTime?>(student.Firstname, student.SpecialDate)
								  into g
								  select new { Count = g.Count(), Key = g.Key, Name = g.Key.Item1 };

				var results2 = from student in this.model.Students.ToList()
							  group student by new Tuple<string, DateTime?>(student.Firstname, student.SpecialDate)
								  into g
							  select new { Count = g.Count(), Key = g.Key, Name = g.Key.Item1 };

				var resultsArray1 = results1.OrderBy(c => c.Key.Item1).ToArray();
				var resultsArray2 = results2.OrderBy(c => c.Key.Item1).ToArray();

				Assert.IsTrue(resultsArray1.SequenceEqual(resultsArray2));

				scope.Complete();
			}
		}
		
		[Test]
		public virtual void Test_Query_GroupBy_Complex_And_OrderBy_Key2()
		{
			using (var scope = NewTransactionScope())
			{
				var results1 = from student in this.model.Students
							  group student by new { student.Firstname, student.SpecialDate }
								  into g
								  select new { Count = g.Count(), g.Key, Name = g.Key.Firstname };

				var results2 = from student in this.model.Students.ToList()
							   group student by new { student.Firstname, student.SpecialDate }
								  into g
							   select new { Count = g.Count(), g.Key, Name = g.Key.Firstname };

				var resultsArray1 = results1.OrderBy(c => c.Key).ToArray();
				var resultsArray2 = results2.OrderBy(c => c.Key.Firstname + "," + c.Key.SpecialDate?.Ticks).ToArray();

				Assert.IsTrue(resultsArray1.SequenceEqual(resultsArray2));

				scope.Complete();
			}
		}

		private class TempObject
		{
			private readonly TestDataAccessModel model;

			public TempObject(TestDataAccessModel model)
			{
				this.model = model;
			}

			public Address GetAddress()
			{
				return this.model.Address.First(c => c.Number == 178);
			}
		}

		[Test]
		public virtual void Test_Query_With_Local_Object_And_Method_Call()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
							  where student.Address == new TempObject(this.model).GetAddress()
							  orderby student.Firstname, student.Id
							  select new { student };

				var results2 = from student in this.model.Students.Include(c => c.Address).ToList()
							  where student.Address == new TempObject(this.model).GetAddress()
							  orderby student.Firstname, student.Id
							  select new { student };

				Assert.IsTrue(results.ToList().SequenceEqual(results2));

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_With_Multiple_From_Manual_Join()
		{
			using (var scope = NewTransactionScope())
			{
				var results = (from student in this.model.Students
							  from school in this.model.Schools
							  orderby  student.Firstname, student.Lastname
							  where student.School.Id == school.Id
							  select new Tuple<string, string>(student.Fullname, school.Name)).ToList();

				var students = this.model.Students.ToArray();
				var schools = this.model.Schools.ToArray();

				var resultsLocal = (from student in students
									from school in schools
									orderby student.Firstname, student.Lastname
									where student.School.Id == school.Id
									select new Tuple<string, string>(student.Fullname, school.Name)).ToList();

				Assert.IsTrue(resultsLocal.SequenceEqual(results));

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Left_Outer_Join1()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from school in this.model.Schools
					join student in this.model.Students on school.Id equals student.School.Id into studentgroup
					from student in studentgroup.DefaultIfEmpty()
					where school.Name == "Empty school"
					select new { school, student };

				var result = query.ToList();

				Assert.That(result.Count, Is.EqualTo(1));
				Assert.That(result[0].school.Name, Is.EqualTo("Empty school"));
				Assert.That(result[0].student, Is.Null);
			}
		}

		[Test]
		public virtual void Test_Left_Outer_Join2()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from school in this.model.Schools
					from student in this.model.Students.Where(c => c.School == school && school.Name == "Empty school").DefaultIfEmpty()
					select new { school, student };

				var query2 =
					from school in this.model.Schools.ToList()
					from student in this.model.Students.ToList().Where(c => c.School == school && school.Name == "Empty school").DefaultIfEmpty()
					select new { school, student };

				var result = query.ToList();

				Assert.That(result.Count, Is.EqualTo(query2.ToList().Count));
			}
		}

		[Test]
		public virtual void Test_Left_Outer_Join3()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from school in this.model.Schools
					from student in this.model.Students.Where(c => c.School == school).DefaultIfEmpty()
					where school.Name == "Empty school"
					select new { school, student };

				var result = query.ToList();

				Assert.That(result.Count, Is.EqualTo(1));
				Assert.That(result[0].school.Name, Is.EqualTo("Empty school"));
				Assert.That(result[0].student, Is.Null);
			}
		}

		[Test]
		public virtual void Test_Left_Outer_Join4()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from school in this.model.Schools
					from student in this.model.Students.DefaultIfEmpty()
					where student.School == school && school.Name == "Empty school"
					select new { school, student };

				var result = query.ToList();
				
				Assert.That(result.Count, Is.EqualTo(0));
			}
		}

		[Test]
		public virtual void Test_Select_Many_With_Linq_Syntax()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from school in this.model.Schools
					from student in this.model.Students
					where school.Name == "Empty school"
					select new { school, student };

				var result = query.ToList();
			}
		}

		[Test]
		public virtual void Test_Select_Many_With_Linq_Syntax_And_DefaultIfEmpty()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from school in this.model.Schools
					from student in this.model.Students
					where school.Name == "Empty school"
					select new { school, student };

				var result = query.ToList();
			}
		}


		[Test]
		public virtual void Test_SelectMany_With_DefaultIfEmpty()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from school in this.model.Schools
					from student in this.model.Students.DefaultIfEmpty()
					where school.Name == "Empty school"
					select new { school, student };

				var result = query.ToList();

				Assert.That(result.Count, Is.GreaterThan(1));
				Assert.IsTrue(result.All(c => c.school.Name == "Empty school"));
			}
		}

		[Test]
		public virtual void Test_Group_Join()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from school in this.model.Schools
					join student in this.model.Students.DefaultIfEmpty() on school equals student.School into studentgroup
					where school.Name == "Empty school"
					select new { school, studentgroup };

				var result = query.ToList();

				Assert.That(result.Count, Is.EqualTo(1));
				Assert.That(result[0].school.Name, Is.EqualTo("Empty school"));
				Assert.That(result[0].studentgroup, Is.Empty);
			}
		}

		[Test]
		public virtual void Test_Query_With_Skip()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students.ToList();

				var results = ((from student in this.model.Students
								orderby student.Firstname, student.Id
								select student).Skip(2)).ToArray();

				Assert.IsTrue(students.OrderBy(c => c.Firstname).ThenBy(c => c.Id).Skip(2).SequenceEqual(results));
			}
		}

		[Test]
		public virtual void Test_Query_With_Skip_No_Select()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students.ToList();

				var results = this.model.Students.Skip(0);

				Assert.Greater(results.ToList().Count, 0);
			}
		}

		[Test]
		public virtual void Test_Query_With_Take_No_Select()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students.ToList();

				var results = this.model.Students.Take(1);

				Assert.AreEqual(1, results.ToList().Count);
			}
		}

		[Test]
		public virtual void Test_Query_With_Skip_Take_No_Select()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students.ToList();

				var results = this.model.Students.Skip(0).Take(1);

				Assert.AreEqual(1, results.ToList().Count);
			}
		}

		[Test]
		public virtual void Test_Query_With_OrderBy_And_Take()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students.ToList();

				var results = ((from student in this.model.Students
								orderby student.Firstname, student.Id
								select student).Take(2));

				Assert.IsTrue(students.OrderBy(c => c.Firstname).ThenBy(c => c.Id).Take(2).SequenceEqual(results));
			}
		}

		[Test]
		public virtual void Test_Query_With_OrderBy_With_Implicit_Join_And_ThenBy_And_Take1()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students
					.Include(c => c.School)
					.Include(c => c.BestFriend)
					.ToList();

				var results = this.model.Students
					.OrderByDescending(c => c.School.Name)
					.ThenByDescending(c => c.BestFriend.Lastname)
					.ThenByDescending(c => c.Id)
					.Skip(1)
					.Take(2)
					.ToList();

				var dbResults = students
					.OrderByDescending(c => c.School.Name)
					.ThenByDescending(c => c.BestFriend?.Lastname ?? null)
					.ThenByDescending(c => c.Id)
					.Skip(1)
					.Take(2)
					.ToList();

				Assert.IsTrue(dbResults.SequenceEqual(results));
			}
		}

		[Test]
		public virtual void Test_Query_With_OrderBy_With_Implicit_Join_And_ThenBy_And_Take2()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students
					.Include(c => c.School)
					.Include(c => c.BestFriend)
					.ToList();

				var results = this.model.Students
					.OrderBy(c => c.School.Name)
					.ThenByDescending(c => c.BestFriend.Lastname)
					.ThenBy(c => c.Id)
					.Skip(1)
					.Take(2)
					.ToList();

				Assert.IsTrue(students.OrderBy(c => c.School.Name).ThenByDescending(c => c.BestFriend?.Lastname ?? null).ThenBy(c => c.Id).Skip(1).Take(2).SequenceEqual(results));
			}
		}

		[Test]
		public virtual void Test_Query_With_OrderBy_And_Skip_Take1()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students.ToList();

				var results = ((from student in this.model.Students
								orderby student.Firstname, student.Id
								select student).Skip(1).Take(2)).ToList();

				Assert.AreEqual(2, results.Count);

				Assert.IsTrue(results.SequenceEqual(students.OrderBy(c => c.Firstname).ThenBy(c => c.Id).Skip(1).Take(2)));
			}
		}

		[Test]
		public virtual void Test_Query_With_OrderBy_And_Skip_Take2()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students.Include(c => c.School).ToList();

				var results = ((from student in this.model.Students
								orderby student.School.Name, student.Id
								select student).Skip(1).Take(2)).ToList();

				Assert.AreEqual(2, results.Count);

				Assert.IsTrue(results.SequenceEqual(students.OrderBy(c => c.School.Name).ThenBy(c => c.Id).Skip(1).Take(2)));
			}
		}

		[Test]
		public virtual void Test_Query_With_OrderBy_And_Skip_Take3()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students.Include(c => c.School).ToList();

				var results = ((from student in this.model.Students
								orderby student.Firstname, student.School.Name, student.Id
								select student).Skip(1).Take(2)).ToList();

				Assert.AreEqual(2, results.Count);

				Assert.IsTrue(results.SequenceEqual(students.OrderBy(c => c.Firstname).ThenBy(c => c.School.Name).ThenBy(c => c.Id).Skip(1).Take(2)));
			}
		}
		
		[Test]
		public virtual void Test_RelatedDataAccessObjects_Contains0()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.First(c => c.Students.Any());
				var student = school.Students.First();

				var school2 = this.model.Schools.First(c => c.Students.Select(d => d.Id).Contains(student.Id));

				Assert.AreEqual(school, school2);
			}
		}

		[Test]
		public virtual void Test_RelatedDataAccessObjects_Contains1()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.First(c => c.Students.Any());
				var student = school.Students.First();

				var school2 = this.model.Schools.First(c => c.Students.Contains(student));

				Assert.AreEqual(school, school2);
			}
		}
		
		[Test]
		public virtual void Test_RelatedDataAccessObjects_Contains2()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.First(c => c.Students.Any());
				var student = school.Students.First();

				var school2 = this.model.Schools.First(c => c == school && c.Students.Any(d => d == student));

				Assert.AreEqual(school, school2);
			}
		}
		
		[Test]
		public virtual void Test_RelatedDataAccessObjects_Contains3()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.First(c => c.Students.Any());
				var student = school.Students.First();

				var school2 = this.model.Schools.FirstOrDefault(c => c.Students.Any(d => d == student && false));

				Assert.AreEqual(null, school2);
			}
		}

		[Test]
		public virtual void Test_Enum_List_Contains()
		{
			var list = new List<Sex>
			{
				Sex.Male,
				Sex.Female
			};

			using (var scope = NewTransactionScope())
			{
				var count = this.model.Students.Count(c => list.Contains(c.Sex));

				Assert.That(count, Is.GreaterThan(0));
			}
		}

		[Test]
		public virtual void Test_Enum_Array_Contains()
		{
			var list = new []
			{
				Sex.Male,
				Sex.Female
			};

			using (var scope = NewTransactionScope())
			{
				var count = this.model.Students.Count(c => list.Contains(c.Sex));

				Assert.That(count, Is.GreaterThan(0));
			}
		}

		[Test]
		public virtual void Test_Enum_Enumerable_Contains()
		{
			var list = (IEnumerable<Sex>)new List<Sex>
			{
				Sex.Male,
				Sex.Female
			};

			using (var scope = NewTransactionScope())
			{
				var count = this.model.Students.Count(c => list.Contains(c.Sex));

				Assert.That(count, Is.GreaterThan(0));
			}
		}


		[Test]
		public virtual void Test_Negate()
		{
			using (var scope = NewTransactionScope())
			{
				var negativeHeights = this.model
					.Students
					.Select(c => -c.Height)
					.ToList();

				Assert.IsTrue(negativeHeights.All(c => c <= 0));
			}
		}

		[Test]
		public virtual void Test_ServerDateTime()
		{
			if (this.ProviderName.Contains("MySql"))
			{
				return;
			}

			using (var scope = NewTransactionScope())
			{
				var count = this.model.Students.Count(c => c.SpecialDate <= ServerDateTime.Now);
			}

			using (var scope = NewTransactionScope())
			{
				var count = this.model.Students.Count(c => c.SpecialDate <= ServerDateTime.UtcNow.AddMinutes(c.Height));
			}

			using (var scope = NewTransactionScope())
			{
				var count = this.model.Students.Count(c => c.SpecialDate <= ServerDateTime.UtcNow.AddMilliseconds(c.Height));
			}

			using (var scope = NewTransactionScope())
			{
				this.model
					.Students
					.Where(c => c.SpecialDate != null)
					.Select(c => new
					{
						Original = c.SpecialDate.Value,
						Added = c.SpecialDate.Value.AddMilliseconds(1000)
					}).ToList()
					.ForEach(c => Assert.AreEqual(c.Original.AddMilliseconds(1000), c.Added));
			}
		}

		[Test]
		public virtual void Test_Nullable_Enum_Check()
		{
			using (var scope = NewTransactionScope())
			{
				var count = this.model.Students.Count(c => c.SexOptional == Sex.Male);

				Assert.AreEqual(0, count);
			}

			using (var scope = NewTransactionScope())
			{
				var count = this.model.Students.Count(c => c.Sex == Sex.Male);

				Assert.GreaterOrEqual(count, 1);

				var male = this.model.Students.First(c => c.Sex == Sex.Male);

				male.SexOptional = Sex.Male;

				scope.Flush();

				count = this.model.Students.Count(c => c.SexOptional == Sex.Male);

				Sex? sex = null;
				count = this.model.Students.Count(c => c.SexOptional == sex);

				Assert.AreEqual(this.model.Students.Count(c => c.SexOptional == null), count);

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Update_Enum()
		{
			using (var scope = NewTransactionScope())
			{
				var student = this.model.Students.First(c => c.Sex == Sex.Male);

				student.Sex = Sex.Female;

				scope.Flush();

				student = this.model.Students.First(c => c.Id == student.Id);

				Assert.AreEqual(Sex.Female, student.Sex);
			}
		}

		[Test]
		public virtual void Test_Update_Enum_Using_DataAccessScope()
		{
			using (var scope = new DataAccessScope())
			{
				var student = this.model.Students.First(c => c.Sex == Sex.Male);

				student.Sex = Sex.Female;

				scope.Flush();

				student = this.model.Students.First(c => c.Id == student.Id);

				Assert.AreEqual(Sex.Female, student.Sex);
			}
		}

		protected readonly Sex TestSex = Sex.Male;

		[Test]
		public virtual void Test_Check_Enum()
		{
			using (var scope = NewTransactionScope())
			{
				var student = this.model.Students.Where(c => c.Sex == this.TestSex).ToList();
			}
		}

		[Test]
		public virtual void Test_Get_Advanced_Computed_Property_With_AutoIncrement_Guid()
		{
			using (var scope = NewTransactionScope())
			{
				var tum = this.model.Students.FirstOrDefault(c => c.Firstname == "Tum");
				
				var student = this.model.Students.FirstOrDefault(c => c.Urn == "urn:student:" + tum.Id.ToString("N"));

				Assert.AreSame(tum, student);
			}
		}

		[Test]
		public virtual void Test_Get_Advanced_Computed_Property_With_AutoIncrement_Long()
		{
			using (var scope = NewTransactionScope())
			{
				var school1 = this.model.Schools.First();
				var school2 = this.model.Schools.First(c => c.Urn == "urn:school:" + school1.Id);

				Assert.AreSame(school1, school2);
			}
		}

		[Test]
		public virtual void Test_Select_With_Ternary_Operator()
		{
			using (var scope = NewTransactionScope())
			{
				var x = 1;
				var y = 2;

				var school = this.model.Schools.FirstOrDefault(c => x == y ? c.Id == 1 : c.Id == 2);

				Assert.AreEqual(2, school.Id);

				x = 1;
				y = 1;

				school = this.model.Schools.FirstOrDefault(c => x * 2 == y + y ? c.Id == 1 : c.Id == 2);

				Assert.AreEqual(1, school.Id);
			}
		}

		[Test]
		public virtual void Test_Select_FirstOrDefault()
		{
			using (var scope = NewTransactionScope())
			{
				var student = this.model.Students.FirstOrDefault();

				Assert.IsNotNull(student);
			}
		}

		[Test]
		public virtual void Test_Select_FirstOrDefaultAsync()
		{
			var waiter = __Test_Select_FirstOrDefaultAsync().ContinueOnAnyContext().GetAwaiter();

			waiter.GetResult();
		}

		public virtual async Task __Test_Select_FirstOrDefaultAsync()
		{
			using (var scope = new DataAccessScope())
			{
				var student = await this.model.Students.FirstOrDefaultAsync();

				Assert.IsNotNull(student);
			}
		}

		[Test]
		public virtual void Test_Select_FirstOrDefaultAsync2()
		{
			Task.Run(async () => await __Test_Select_FirstOrDefaultAsync2().ContinueOnAnyContext()).Wait();
		}

		public virtual async Task __Test_Select_FirstOrDefaultAsync2()
		{
			var list = await this.model.Students.AsQueryable().ToListAsync();

			Assert.IsNotNull(list);

			var student = this.model.Students.FirstOrDefault();

			Assert.IsNotNull(student);

			list = await this.model.Students.AsQueryable().ToListAsync();

		}

		[Test]
		public virtual void Test_Select_FirstOrDefaultAsyncWithPredicate1()
		{
			Func<Task> func = async () =>
			{
				using (var scope = new DataAccessScope())
				{
					var student = await this.model.Students.FirstOrDefaultAsync(c => c.Firstname == "Tum");

					Assert.IsNotNull(student);
				}
			};

			var waiter = func().ContinueOnAnyContext().GetAwaiter();

			waiter.GetResult();
		}

		[Test]
		public virtual void Test_Select_FirstOrDefaultAsyncWithPredicate2()
		{
			Func<Task> func = async () =>
			{
				using (var scope = new DataAccessScope())
				{
					var student = await this.model.Students.FirstOrDefaultAsync(c => c.Firstname == "aaaaaazzzzTum");

					Assert.IsNull(student);
				}
			};

			var waiter = func().ContinueOnAnyContext().GetAwaiter();

			waiter.GetResult();
		}
	   
		[Test]
		public virtual void Test_ToList()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students.ToList();

				Assert.Greater(students.Count, 0);
			}
		}

		[Test]
		public virtual void Test_ToListAsync()
		{
			var awaiter = __Test_ToListAsync().ContinueOnAnyContext().GetAwaiter();

			awaiter.GetResult();
		}

		public virtual async Task __Test_ToListAsync()
		{
			using (var scope = new DataAccessScope())
			{
				var students = await this.model.Students.AsQueryable().ToListAsync();

				Assert.Greater(students.Count, 0);
			}
		}

		[Test]
		public virtual void Test_Select_Many_Students_From_Schools1()
		{
			using (var scope = NewTransactionScope())
			{
				var query = this.model.Schools
					.Where(c => c.Name == "Bruce's Kung Fu School")
					.SelectMany(c => c.Students);

				var results = query.ToList();
			}
		}

		[Test]
		public virtual void Test_Select_Many_Students_From_Schools2()
		{
			using (var scope = NewTransactionScope())
			{
				var query = this.model.Schools
					.Where(c => c.Name == "Bruce's Kung Fu School")	
					.SelectMany(c => c.Students).Select(c => c.School.Name);

				var results = query.ToList();
			}
		}

		[Test]
		public virtual void Test_Select_Many_Students_From_Schools3()
		{
			using (var scope = NewTransactionScope())
			{
				var query = this.model.Schools
					.Where(c => c.Name == "Invalid School")
					.DefaultIfEmpty()
					.Include(c => c.Address)
					.SelectMany(c => c.Students.DefaultIfEmpty(), (s, c) => new {s, c});

				var list = query.ToList();

				Assert.AreEqual(1, list.Count);
				Assert.IsNull(list[0].s);
				Assert.IsNull(list[0].c);
			}

			if (this.model.GetCurrentSqlDatabaseContext().SqlDialect.SupportsCapability(Persistence.SqlCapability.CrossApply))
			{
				using (var scope = NewTransactionScope())
				{
					var query = this.model.Schools
						.Where(c => c.Name == "Bruce's Kung Fu School")
						.DefaultIfEmpty()
						.SelectMany(c => c.Students.DefaultIfEmpty().Include(d => d.Cats), (s, c) => new { s, c });

					var list = query.ToList();

					Assert.AreEqual(2, list.Count);
					Assert.IsNotNull(list[0].s);
					Assert.IsNotNull(list[0].c);
					Assert.IsNotNull(list[1].s);
					Assert.IsNotNull(list[1].c);
					Assert.AreSame(list[0].s, list[1].s);
					Assert.AreNotEqual(list[0].c, list[1].c);
				}
			}

			using (var scope = NewTransactionScope())
			{
				var query = this.model.Schools
					.Where(c => c.Name == "Empty school")
					.DefaultIfEmpty()
					.Include(c => c.Address)
					.SelectMany(c => c.Students.DefaultIfEmpty(), (s, c) => new { s, c });

				var list = query.ToList();

				Assert.AreEqual(1, list.Count);
				Assert.IsNotNull(list[0].s);
				Assert.IsNull(list[0].c);
			}

			using (var scope = NewTransactionScope())
			{
				var query = this.model.Schools
					.Where(c => c.Name == "Bruce's Kung Fu School")
					.DefaultIfEmpty()
					.SelectMany(c => c.Students.DefaultIfEmpty(), (s, c) => new { s, c });

				var list = query.ToList();

				Assert.AreEqual(2, list.Count);
				Assert.IsNotNull(list[0].s);
				Assert.IsNotNull(list[0].c);
				Assert.IsNotNull(list[1].s);
				Assert.IsNotNull(list[1].c);
				Assert.AreSame(list[0].s, list[1].s);
				Assert.AreNotEqual(list[0].c, list[1].c);
			}
		}

		[Test]
		public virtual void Test_Select_Many_Students_From_Schools5()
		{
			if (this.model.GetCurrentSqlDatabaseContext().SqlDialect.SupportsCapability(Persistence.SqlCapability.CrossApply))
			{
				using (var scope = NewTransactionScope())
				{
					var query = this.model.Schools
						.Where(c => c.Name == "Bruce's Kung Fu School")
						.DefaultIfEmpty()
						.SelectMany(c => c.Students.DefaultIfEmpty().Include(d => d.Cats), (s, c) => new { s, c });

					var list = query.ToList();

					Assert.AreEqual(2, list.Count);
					Assert.IsNotNull(list[0].s);
					Assert.IsNotNull(list[0].c);
					Assert.IsNotNull(list[1].s);
					Assert.IsNotNull(list[1].c);
					Assert.AreSame(list[0].s, list[1].s);
					Assert.AreNotEqual(list[0].c, list[1].c);
				}
			}
		}

		[Test]
		public virtual void Test_Select_Many_Students_From_Schools6()
		{
			if (this.model.GetCurrentSqlDatabaseContext().SqlDialect.SupportsCapability(Persistence.SqlCapability.CrossApply))
			{
				using (var scope = NewTransactionScope())
				{
					var query = this.model.Schools
						.Where(c => c.Name == "Bruce's Kung Fu School")
						.DefaultIfEmpty()
						.SelectMany(c => c.Students.DefaultIfEmpty().Include(d => d.Cats), (s, c) => new object[] { s, c });

					var list = query.ToList();

					Assert.AreEqual(2, list.Count);
					Assert.IsNotNull(list[0][0]);
					Assert.IsNotNull(list[0][1]);
					Assert.IsNotNull(list[1][0]);
					Assert.IsNotNull(list[1][1]);
					Assert.AreSame(list[0][0], list[1][0]);
					Assert.AreNotEqual(list[0][1], list[1][1]);
				}
			}
		}

		[Test]
		public virtual void Test_Select_Many_Students_From_Schools4()
		{
			using (var scope = NewTransactionScope())
			{
				var ids = new List<long> { 0, 1, 2, 3, 4, 5 };

				var query = this.model.Schools.Where(c => c.Name == "blahblah").DefaultIfEmpty();

				Assert.AreEqual(1, query.ToList().Count);
			}
		}

		[Test]
		public virtual void Test_Select_Many_Students_Implicit_Join_Inside_Select_Inside_SelectMany()
		{
			using (var scope = NewTransactionScope())
			{
				var query = this.model.Schools
					.Where(c => c.Name == "Bruce's Kung Fu School")
					.SelectMany(c => c.Students.Select(d => d.Address.Number));

				var results = query.ToList();
			}
		}

		[Test]
		public virtual void Test_Select_With_Implicit_GroupJoin1()
		{
			using (var scope = NewTransactionScope())
			{
				var query = this.model.Schools
					.Where(c => c.Name == "Bruce's Kung Fu School")
					.Select(c => c.Students);

				var results = query.ToList();
				var subResults = results.First().ToList();
			}
		}

		[Test]
		public virtual void Test_Select_With_Implicit_GroupJoin2()
		{
			using (var scope = NewTransactionScope())
			{
				var query = this.model.Schools
					.Where(c => c.Name == "Bruce's Kung Fu School")
					.Select(c => c.Students);

				var results = query.ToList();
				var subResults = results.First().ToList();
			}
		}

		[Test]
		public virtual void Test_Select_Many_With_Implicit_GroupJoin2()
		{
			using (var scope = NewTransactionScope())
			{
				var query = this.model.Schools
					.Where(c => c.Name == "Bruce's Kung Fu School")
					.SelectMany(c => c.Students.Select(d => d.Firstname));

				var results = query.ToList();
			}
		}

		[Test]
		public virtual void Test_Select_Many_Students_From_Schools_Manual()
		{
			using (var scope = NewTransactionScope())
			{
				var query = this.model.Schools
					.Where(c => c.Name == "Bruce's Kung Fu School")
					.SelectMany(c => this.model.Students,
						(school, student) => new
						{
							school,
							student
						}).Where(c => c.student.School == c.school);

				var results = query.ToList();
			}
		}

		[Test]
		public virtual void Test_From_Multiple_Times()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from student in this.model.Students
					from school in this.model.Schools
					from product in this.model.Products
					from product2 in this.model.Products
					select new
					{
						student,
						school,
						product,
						product2
					};

				var results = query.ToList();
			}
		}

		[Test]
		public virtual void Test_Select_With_Scope()
		{
			using (var scope = NewTransactionScope())
			{
				var student1 = this.model.Students.Single(c => c.Firstname == "Tum");
				var student2 = this.model.Students.Single(c => c.Firstname == "Tum");

				Assert.AreSame(student1, student2);
			}
		}

		[Test]
		public virtual void Test_Select_Without_Scope()
		{
			var student1 = this.model.Students.Single(c => c.Firstname == "Tum");
			var student2 = this.model.Students.Single(c => c.Firstname == "Tum");

			Assert.AreNotSame(student1, student2);
		}

		[Test]
		public void Test_Query_Related_Objects1()
		{
			using (var scope = NewTransactionScope())
			{
				var brucesSchool = this.model.Schools.First(c => c.Name.Contains("Bruce"));
				var brandonsSchool = this.model.Schools.First(c => c.Name.Contains("Brandon"));

				Assert.IsNotNull(brucesSchool.Students.Single(c => c.Firstname == "Tum"));
				Assert.IsNull(brandonsSchool.Students.FirstOrDefault(c => c.Firstname == "Tum"));
			}
		}

		[Test]
		public void Test_Query_Related_Objects2()
		{
			using (var scope = NewTransactionScope())
			{
				var studentCountBySchoolId = this.model.Schools.ToList().ToDictionary(c => c.Id, c => c.Students.Count());

				foreach (var school in this.model.Schools.ToList() /* MySql ADO provider doesn't allow nested Count below */)
				{
					var expected = studentCountBySchoolId[school.Id];

					Assert.AreEqual(expected, school.Students.Count());
					Assert.AreEqual(expected, this.model.Students.Count(c => c.School == school));
				}
			}
		}

		[Test]
		[Category("IgnoreOnMono")]
		/* Currently fails with Mono */
		public void Test_Query_Related_Objects3()
		{
			Func<Task> func = async () =>
			{
				using (var scope = NewTransactionScope())
				{
					var studentCountBySchoolId = this.model.Schools.ToList().ToDictionary(c => c.Id, c => c.Students.Count());

					foreach (var school in this.model.Schools)
					{
						var expected = studentCountBySchoolId[school.Id];

						Assert.AreEqual(expected, await school.Students.CountAsync());
						Assert.AreEqual(expected, await this.model.Students.CountAsync(c => c.School == school));
					}
				}
			};

			func().GetAwaiter().GetResult();
		}

		[Test]
		public void Test_Query_First1()
		{
			Assert.IsNotNull(this.model.Students.First());
		}

		[Test]
		public void Test_Query_Single1()
		{
			var student = this.model.Students.Single(c => c.Firstname == "Tum");

			Assert.AreEqual("Tum", student.Firstname);
		}

		[Test]
		public void Test_Query_Check_Has_Changed1()
		{
			var student = this.model.Students.First();

			Assert.IsFalse(((IDataAccessObjectAdvanced)student).HasObjectChanged);
		}

		[Test]
		public void Test_Query_Check_Has_Changed2()
		{
			var studentId = Guid.Empty;

			using (var scope = NewTransactionScope())
			{
				var student = this.model.Schools.SingleOrDefault(c => c.Name == "Bruce's Kung Fu School").Students.Create();

				scope.Flush();

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				foreach (var student in this.model.Students.OrderBy(c => c.Firstname))
				{
					Assert.That(((IDataAccessObjectAdvanced)student).GetChangedPropertiesFlattened(), Is.Empty);

					student.Sex = student.Sex;
					student.School = student.School;
					student.Fraternity = student.Fraternity;
					student.BestFriend = student.BestFriend;
					student.Address = student.Address;

					student.Firstname = student.Firstname;
					student.Email = student.Email;
					student.Lastname = student.Lastname;
					student.Nickname = student.Nickname;
					student.Height = student.Height;
					student.Weight = student.Weight;
					student.FavouriteNumber = student.FavouriteNumber;
					student.SpecialDate = student.SpecialDate;

					Assert.That(((IDataAccessObjectAdvanced)student).GetChangedPropertiesFlattened(), Is.Empty);
					Assert.IsFalse(((IDataAccessObjectAdvanced)student).HasObjectChanged);
				}
			}

			using (var scope = NewTransactionScope())
			{
				this.model.Students.Delete(c => c.Id == studentId);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_First2()
		{
			using (var scope = NewTransactionScope())
			{
				var student1 = this.model.Students.Where(c => c.Firstname == "Tum").First();
				var student2 = this.model.Students.First(c => c.Firstname == "Tum");

				Assert.AreSame(student1, student2);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_First3()
		{
			Assert.Catch<InvalidOperationException>(() =>
			{
				using (var scope = NewTransactionScope())
				{
					var student1 = this.model.Students.First(c => c.Firstname == "iewiorueo");

					scope.Complete();
				}
			});
		}

		[Test]
		public void Test_Query_FirstOrDefault1()
		{
			using (var scope = NewTransactionScope())
			{
				var student1 = this.model.Students.FirstOrDefault(c => c.Firstname == "iewiorueo");

				Assert.IsNull(student1);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_FirstOrDefault2()
		{
			using (var scope = NewTransactionScope())
			{
				var student1 = this.model.Students.FirstOrDefault(c => c.Firstname == "Tum");

				Assert.IsNotNull(student1);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_Select_Scalar_With_First()
		{
			using (var scope = NewTransactionScope())
			{
				var studentName = this.model.Students.Select(c => c.Firstname).First(c => c != null);

				Assert.IsNotNull(studentName);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_With_Where1()
		{
			using (var scope = NewTransactionScope())
			{
				var students = from student in this.model.Students
							  where student.Firstname == "Tum" && student.Lastname == "Nguyen"
							  select student;

				Assert.AreEqual(1, students.Count());
				Assert.IsNotNull(students.FirstOrDefault());
			}
		}

		[Test]
		public void Test_Query_With_Where2()
		{
			using (var scope = NewTransactionScope())
			{
				var students = from student in this.model.Students
							   where (student.Firstname == "A" && student.Lastname == "B")
									|| student.Fullname == "Tum Nguyen"
							   select student;

				Assert.AreEqual(1, students.Count());
				Assert.IsNotNull(students.FirstOrDefault());
			}
		}


		[Test]
		public void Test_Query_With_Where3()
		{
			using (var scope = NewTransactionScope())
			{
				var students = from student in this.model.Students
							   where student.Firstname == "A" && (student.Lastname == "B" || student.Fullname == "Tum Nguyen")
							   select student;

				Assert.AreEqual(0, students.Count());
			}
		}

		[Test]
		public void Test_Query_With_Greater_Than1()
		{
			using (var scope = NewTransactionScope())
			{
				var students = from student in this.model.Students
						  where student.Firstname == "Tum" && student.Height > 170
						  select student;

				Assert.AreEqual(1, students.Count());
				Assert.IsNotNull(students.FirstOrDefault());
			}
		}

		[Test]
		public void Test_Query_With_Greater_Than2()
		{
			using (var scope = NewTransactionScope())
			{
				var students = from student in this.model.Students
							   where student.Firstname == "Tum" && student.Height > 177
							   select student;

				Assert.AreEqual(0, students.Count());
			}
		}

		[Test]
		public void Test_Query_With_Less_Than1()
		{
			using (var scope = NewTransactionScope())
			{
				var students = from student in this.model.Students
							   where student.Firstname == "Tum" && student.Height < 178
							   select student;

				Assert.AreEqual(1, students.Count());
				Assert.IsNotNull(students.FirstOrDefault());
			}
		}

		[Test]
		public void Test_Query_With_Less_Than2()
		{
			using (var scope = NewTransactionScope())
			{
				var students = from student in this.model.Students
							   where student.Firstname == "Tum" && student.Height < 177
							   select student;

				Assert.AreEqual(0, students.Count());
			}
		}

		[Test]
		public void Test_Query_With_Less_Than3()
		{
			using (var scope = NewTransactionScope())
			{
				var students = from student in this.model.Students
							   where student.Firstname == "Tum" && student.Height < 177.001
							   select student;

				Assert.AreEqual(1, students.Count());
				Assert.IsNotNull(students.FirstOrDefault());
			}
		}

		[Test]
		public void Test_Query_With_Less_Than4()
		{
			using (var scope = NewTransactionScope())
			{
				var students = from student in this.model.Students
							   where student.Firstname == "Tum" && student.Height < 174.999
							   select student;

				Assert.AreEqual(0, students.Count());
			}
		}

		[Test]
		public virtual void Test_Query_With_GroupBy_Max_And_Average_And_Sum()
		{
			using (var scope = NewTransactionScope())
			{
				var product1 = this.model.Products.Create();

				product1.Name = "Uniform";
				product1.Price = 150;

				var product2 = this.model.Products.Create();

				product2.Name = "Belt";
				product2.Price = 22;

				var product3 = this.model.Products.Create();

				product3.Name = "Belt";
				product3.Price = 56;

				scope.Flush();

				var results =
					from product in this.model.Products
					group product by product.Name
						into g
						select new
						{
							Name = g.Key,
							Min = g.Min(p => p.Price),
							Max = g.Max(p => p.Price),
							Average = g.Average(p => p.Price),
							Sum = g.Sum(p => p.Price)
						};

				var resultsArray = results.OrderBy(c => c.Name).ToArray();

				Assert.AreEqual(2, resultsArray.Length);
				Assert.AreEqual("Belt", resultsArray[0].Name);
				Assert.AreEqual((product2.Price + product3.Price) / 2, resultsArray[0].Average);
				Assert.AreEqual((product2.Price + product3.Price), resultsArray[0].Sum);
				Assert.AreEqual(Math.Min(product2.Price, product3.Price), resultsArray[0].Min);
				Assert.AreEqual(Math.Max(product2.Price, product3.Price), resultsArray[0].Max);

				Assert.AreEqual("Uniform", resultsArray[1].Name);
				Assert.AreEqual(product1.Price, resultsArray[1].Average);
				Assert.AreEqual(product1.Price, resultsArray[1].Sum);
				Assert.AreEqual(product1.Price, resultsArray[1].Min);
				Assert.AreEqual(product1.Price, resultsArray[1].Max);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_Aggregate_Sum1()
		{
			using (var scope = NewTransactionScope())
			{
				var totalHeight = this.model.Students.Sum(c => c.Height);

				Assert.That(totalHeight, Is.GreaterThanOrEqualTo(177));
			}
		}

		[Test]
		public void Test_Query_Aggregate_Sum2()
		{
			using (var scope = NewTransactionScope())
			{
				var totalHeight = this.model.Students.Where(c => c.Fullname == "Tum Nguyen").Sum(c => c.Height);

				Assert.AreEqual(177, totalHeight);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Sum3()
		{
			using (var scope = NewTransactionScope())
			{
				var serverSideResult = this.model.Students.Sum(c => c.Height);
				var allStudents = this.model.Students.ToList();
				var clientSideResult = allStudents.Sum(c => c.Height);

				Assert.That(serverSideResult, Is.GreaterThanOrEqualTo(197));
				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Sum4()
		{
			using (var scope = NewTransactionScope())
			{
				var serverSideResult = this.model.Students.Sum(c => c.BankBalance);
				var allStudents = this.model.Students.ToList();
				var clientSideResult = allStudents.Sum(c => c.BankBalance);

				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Average()
		{
			using (var scope = NewTransactionScope())
			{
				var serverSideResult = this.model.Students.Average(c => c.Height);
				var allStudents = this.model.Students.ToList();
				var clientSideResult = allStudents.Average(c => c.Height);

				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_AverageAsync()
		{
			var task = ((Func<Task>)(async () =>
			{
				using (var scope = new DataAccessScope())
				{
					var serverSideResult = await this.model.Students.AverageAsync(c => c.Height);
					var allStudents = this.model.Students.ToList();
					var clientSideResult = allStudents.Average(c => c.Height);

					Assert.AreEqual(clientSideResult, serverSideResult);
				}
			}))();

			Thread.Sleep(20);
			task.GetAwaiter().GetResult();
		}

		[Test]
		public void Test_Query_Aggregate_Average_Complex1()
		{
			using (var scope = NewTransactionScope())
			{
				var serverSideResult = this.model.Students.Average(c => c.Height * 2);
				var allStudents = this.model.Students.ToList();
				var clientSideResult = allStudents.Average(c => c.Height * 2);

				Assert.AreEqual(clientSideResult, serverSideResult);
				Assert.AreEqual(allStudents.Average(c => c.Height) * 2, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Average_Complex2()
		{
			using (var scope = NewTransactionScope())
			{
				var serverSideResult = this.model.Students.Average(c => c.Height + c.FavouriteNumber);
				var allStudents = this.model.Students.ToList();
				var clientSideResult = allStudents.Average(c => c.Height + c.FavouriteNumber);

				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Max()
		{
			using (var scope = NewTransactionScope())
			{
				var serverSideResult = this.model.Students.Max(c => c.Height);
				var allStudents = this.model.Students.ToList();
				var clientSideResult = allStudents.Max(c => c.Height);

				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_MaxAsync()
		{
			var task = ((Func<Task>)(async () =>
			{
				using (var scope = new DataAccessScope())
				{
					var serverSideResult = await this.model.Students.MaxAsync(c => c.Height);
					var allStudents = this.model.Students.ToList();
					var clientSideResult = allStudents.Max(c => c.Height);

					Assert.AreEqual(clientSideResult, serverSideResult);
				}
			}))();

			Thread.Sleep(20);
			task.GetAwaiter().GetResult();
		}

		[Test]
		public void Test_Query_Aggregate_Min()
		{
			using (var scope = NewTransactionScope())
			{
				var serverSideResult = this.model.Students.Min(c => c.Height);
				var allStudents = this.model.Students.ToList();
				var clientSideResult = allStudents.Min(c => c.Height);

				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_MinAsync()
		{
			var task = ((Func<Task>)(async () =>
			{
				using (var scope = new DataAccessScope())
				{
					var serverSideResult = await this.model.Students.MinAsync(c => c.Height);
					var allStudents = this.model.Students.ToList();
					var clientSideResult = allStudents.Min(c => c.Height);

					Assert.AreEqual(clientSideResult, serverSideResult);
				}
			}))();

			Thread.Sleep(20);
			task.GetAwaiter().GetResult();
		}

		[Test]
		public void Test_Query_Aggregate_Sum_With_Complex_Aggregate_Computation()
		{
			using (var scope = NewTransactionScope())
			{
				var totalHeight = this.model.Students.Where(c => c.Fullname == "Tum Nguyen").Sum(c => c.Height * 2);

				Assert.AreEqual(354, totalHeight);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Sum_With_Group1()
		{
			const string name = "Tum";
			var query = from student in this.model.Students
					   where student.Firstname == name
						group student by student.Id
									into g
									select
									g.Sum(x => x.Height);
			
			var sum = query.FirstOrDefault();

			Assert.AreEqual(177, sum);
		}

		[Test]
		public void Test_Query_Aggregate_Sum_With_Group2()
		{
			var tum = this.model.Students.Single(c => c.Firstname == "Tum");
			var mars = this.model.Students.Single(c => c.Firstname == "Mars");

			var sums = (from student in this.model.Students
						group student by student.Id
						into g
						select new
						{
							Sum = g.Sum(x => x.Height),
							Id = g.Key
						}).ToDictionary(c => c.Id, c => c);

			Assert.AreEqual(tum.Height, sums[tum.Id].Sum);
			Assert.AreEqual(mars.Height, sums[mars.Id].Sum);
		}

		[Test]
		public void Test_Query_Aggregate_Sum_With_Group3()
		{
			var students = this.model.Students.ToList();

			var tum = this.model.Students.Single(c => c.Firstname == "Tum");
			var mars = this.model.Students.Single(c => c.Firstname == "Mars");

			var sums = (from student in this.model.Students
						group student by student.Id
							into g
							select new
							{
								Sum = g.Sum(x => x.Height + x.FavouriteNumber),
								Id = g.Key
							}).ToDictionary(c => c.Id, c => c);

			Assert.AreEqual(tum.Height + tum.FavouriteNumber, sums[tum.Id].Sum);
			Assert.AreEqual(mars.Height + mars.FavouriteNumber, sums[mars.Id].Sum);
		}

		[Test]
		public void Test_Query_With_OrderBy()
		{
			var students = (from student in this.model.Students
							orderby student.Firstname
								select student).ToList();

			Assert.That(students.Count, Is.GreaterThan(1));
		}

		[Test]
		public void Test_Query_GroupBy()
		{
			var students = (from student in this.model.Students
							group student by student.Id
								into g
								select new
								{
									g.Key,
									Count = g.Count()
								}).ToList();
		}

		[Test]
		public void Test_Query_GroupBy_Object_And_Select_Object()
		{
			var students = (from student in this.model.Students
							group student by student.School
								into g
							select new
							{
								g.Key
							}).ToList();
		}

		[Test]
		public void Test_Query_GroupBy_Multiple_Values()
		{
			var students = (from student in this.model.Students
							group student by new
							{
								student.Id,
								student.Firstname
							}
							into g
							select new
							{
								g.Key.Id,
								g.Key.Firstname,
								Count = g.Count()
							}).ToList();

			Console.WriteLine(students.Count);
		}

		[Test]
		public virtual void Test_Coalesce_Function()
		{
			using (var scope = NewTransactionScope())
			{
				var tum = (from student in this.model.Students
						   where student.Nickname == ""
								 && student.Firstname == "Tum"
						   select new
						   {
							   Student = student
						   }).FirstOrDefault();

				Assert.IsNull(tum);
			}

			using (var scope = NewTransactionScope())
			{
				var tum = (from student in this.model.Students
						   where (student.Nickname ?? "") == ""
								 && student.Firstname == "Tum"
						   select new
						   {
							   Student = student
						   }).Single();

				Assert.IsNotNull(tum);
			}
		}

		[Test]
		public virtual void Test_Custom_Type_Projection()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.First(c => c.Name.Contains("Bruce"));
				var student1 = school.Students.Where(c => c.Firstname == "Tum").ToList();

				Assert.IsNotNull(student1);
				Assert.AreEqual(1979, student1.First().Birthdate.Value.ToDateTime().Year);
				Assert.AreEqual(0, student1.First().Birthdate.Value.ToDateTime().Hour);
			}
		}

		[Test]
		public virtual void Test_Date_Functions_When_Using_Custom_Date_Data_Type()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students
					.Where(c => c.FavouriteDate.Year == 2016)
					.Where(c => c.FavouriteDate.Month == 8)
					.Where(c => c.FavouriteDate.Day == 19)
					.ToList();

				Assert.AreEqual(1, students.Count);
			}
		}

		[Test]
		public virtual void Test_Custom_Type_Comparison1()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.First(c => c.Name.Contains("Bruce"));
				var student1 = school.Students.First(c => c.Firstname == "Tum" && c.Birthdate < DateTime.Now);
			}
		}

		[Test]
		public virtual void Test_Custom_Type_Comparison2()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.First(c => c.Name.Contains("Bruce"));

				var students = school.Students
					.Select(c => new { c.Firstname, DateTime = (DateTime?)c.Birthdate })
					.ToList();

				Assert.Greater(students.Count, 0);
			}
		}

		[Test]
		public virtual void Test_Custom_Type_Operations1()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.First(c => c.Name.Contains("Bruce"));

				var students = school.Students
					.Where(c => c.Birthdate.Value.ToDateTime().Year == 1979)
					.Where(c => c.Birthdate.Value.ToDateTime().AddYears(10) < DateTime.Now)
					.Select(c => new { c.Firstname })
					.ToList();

				Assert.Greater(students.Count, 0);
			}
		}

		[Test]
		public virtual void Test_Custom_Type_Operations2()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.First(c => c.Name.Contains("Bruce"));

				var students = school.Students
					.Where(c => c.Birthdate.Value.ToDateTime().Year == 1979)
					.Where(c => c.Birthdate.Value.ToDateTime().AddMonths(10) < DateTime.Now)
					.Select(c => new { c.Firstname })
					.ToList();

				Assert.Greater(students.Count, 0);
			}
		}

		[Test]
		public virtual void Test_Custom_Type_Operations3()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.First(c => c.Name.Contains("Bruce"));

				var students = school.Students
					.Where(c => c.Birthdate.Value.ToDateTime().Year == 1979)
					.Where(c => c.Birthdate.Value.ToDateTime().AddDays(10) < DateTime.Now)
					.Select(c => new { c.Firstname })
					.ToList();

				Assert.Greater(students.Count, 0);
			}
		}

		[Test]
		public virtual void Test_GroupBy_Date()
		{
			using (var scope = NewTransactionScope())
			{
				var tum2 = this.model.Schools.First(c => c.Name.Contains("Bruce")).Students.Create();

				tum2.Firstname = "Tum";
				tum2.Lastname = "Nguyen";
				tum2.Height = 177;
				tum2.FavouriteNumber = 36;
				tum2.SpecialDate = new DateTime(1979, 12, 24, 05, 00, 00);

				scope.Flush();

				var group = (from student in this.model.Students
							 group student by student.SpecialDate
								 into g
								 select new
								 {
									 Date = g.Key,
									 Count = g.Count()
								 }).ToList();

				Assert.That(group.Count, Is.GreaterThan(2));

				group = (from student in this.model.Students
						 where student.Firstname == "Tum"
						 group student by student.SpecialDate
							 into g
							 select new
							 {
								 Date = g.Key,
								 Count = g.Count()
							 }).ToList();


				Assert.That(group.Count, Is.EqualTo(2));
				Assert.That(group[0].Count, Is.EqualTo(1));
				Assert.That(group[1].Count, Is.EqualTo(1));
			}

			// Make sure temp student wasn't saved
			using (var scope = NewTransactionScope())
			{
				Assert.AreEqual(1, this.model.Students.Count(c => c.Firstname == "Tum"));
			}
		}

		[Test, Ignore("LogicalNot working on postgres yet")]
		public virtual void Test_GroupBy_Date_With_Date_Only()
		{
			using (var scope = NewTransactionScope())
			{
				var tum2 = this.model.Schools.First(c => c.Name.Contains("Bruce")).Students.Create();

				tum2.Firstname = "Tum";
				tum2.Lastname = "Nguyen";
				tum2.Height = 177;
				tum2.FavouriteNumber = 36;
				tum2.SpecialDate = new DateTime(1979, 12, 24, 05, 00, 00);

				scope.Flush();

				var group = (from student in this.model.Students
							 group student by student.SpecialDate.GetValueOrDefault().Date
							 into g
							 select new
							 {
								 Date = g.Key,
								 Count = g.Count()
							 }).ToList();

				Assert.That(group.Count, Is.GreaterThan(2));

				group = (from student in this.model.Students
						 where student.Firstname == "Tum"
						 group student by student.SpecialDate.GetValueOrDefault().Date
						 into g
						 select new
						 {
							 Date = g.Key,
							 Count = g.Count()
						 }).ToList();


				Assert.That(group.Count, Is.EqualTo(1));
				Assert.That(group[0].Count, Is.EqualTo(2));
			}

			// Make sure temp student wasn't saved
			using (var scope = NewTransactionScope())
			{
				Assert.AreEqual(1, this.model.Students.Count(c => c.Firstname == "Tum"));
			}
		}

		[Test]
		public void Test_Select_Contains()
		{
			using (var scope = NewTransactionScope())
			{
				Assert.IsFalse(this.model.Students
								   .Select(c => c.Lastname)
								   .Contains("zzzzz"));

				Assert.IsTrue(this.model.Students
								   .Select(c => c.Lastname)
								   .Contains("Nguyen"));

				scope.Complete();
			}
		}

		[Test]
		public void Test_Select_List_Contains1()
		{
			var ids = this.model.Students.Select(c => c.Id).OrderBy(c => c).ToList();

			using (var scope = NewTransactionScope())
			{
				var list = this.model.Students.Where(c => ids.Contains(c.Id)).OrderBy(c => c).ToList();

				Assert.AreEqual(ids.Count, list.Count);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Select_List_Contains2()
		{
			var ids = new List<Guid>();

			using (var scope = NewTransactionScope())
			{
				var list = this.model.Students.Where(c => ids.Contains(c.Id)).ToList();

				Assert.IsEmpty(list);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Select_List_Contains3()
		{
			var ids = new List<Guid>();

			using (var scope = NewTransactionScope())
			{
				var list = this.model.Students.Where(c => !ids.Contains(c.Id)).ToList();

				Assert.AreEqual(this.model.Students.Count(), list.Count);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Select_List_Contains4()
		{
			var ids1 = this.model.Students.Select(c => c.Id).Take(1).ToList();
			var ids2 = new List<Guid>();

			using (var scope = NewTransactionScope())
			{
				var list = this.model.Students.Where(c => !c.Overseas && c.SerialNumber1 != 9999991123 && !ids1.Contains(c.Id))
					.Select(c => c.Id)
					.OrderBy(c => c)
					.ToList();

				var list2 = this.model.Students.ToList().Where(c => !c.Overseas && c.SerialNumber1 != 9999991123 && !ids1.Contains(c.Id))
					.Select(c => c.Id)
					.OrderBy(c => c)
					.ToList();

				Assert.IsTrue(list.SequenceEqual(list2));

				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				var list = this.model.Students.Where(c => !c.Overseas && c.SerialNumber1 != 9999991123 && !ids2.Contains(c.Id))
					.Select(c => c.Id)
					.OrderBy(c => c)
					.ToList();

				var list2 = this.model.Students.Where(c => !c.Overseas && c.SerialNumber1 != 9999991123 && !ids2.Contains(c.Id))
					.Select(c => c.Id)
					.OrderBy(c => c)
					.ToList();

				Assert.IsTrue(list.SequenceEqual(list2));

				scope.Complete();
			}
		}

		[Test]
		public void Test_Select_List_Contains5()
		{
			var ids1 = new List<Guid>();
			var ids2 = this.model.Students.Select(c => c.Id).Take(1).OrderBy(c => c).ToList();

			using (var scope = NewTransactionScope())
			{
				var list = this.model.Students.Where(c => c.SerialNumber1 != 9999991123 && !ids1.Contains(c.Id))
					.Select(c => c.Id)
					.OrderBy(c => c)
					.ToList();

				Assert.AreEqual(this.model.Students.Count(), list.Count);

				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				var list = this.model.Students.Where(c => c.SerialNumber1 != 9999991123 && !ids2.Contains(c.Id))
					.Select(c => c.Id)
					.ToList();

				Assert.AreEqual(this.model.Students.Count() - 1, list.Count);

				scope.Complete();
			}
		}

		private class KeyCount
		{
			public DateTime Key { get; set; }
			public int Count { get; set; }
		}

		[Test]
		public void Test_GroupBy_DateTimeDate_Into_StronglyTypedType()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
							  group student by student.SpecialDate.Value.Date
								  into g
								  select new KeyCount
								  {
									  Key = g.Key,
									  Count = g.Count()
								  };

				var list = results.ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Compare_Month()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
					where student.SpecialDate.Value.Date.Year == 12
							  select student;


				var list = results.ToList();

				scope.Complete();
			}
		}


		[Test]
		public void Test_GroupBy_DateTimeDate()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
							  group student by student.SpecialDate.Value.Date
								  into g
								  select new
								  {
									  key = g.Key,
									  count = g.Count()
								  };

				var list = results.ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_GroupBy_AggregateCount1()
		{
			using (var scope = NewTransactionScope())
			{
				var results = (from student in this.model.Students
					where student.School.Id == 1
					group student by student.Firstname
					into g
					select new
					{
						key = g.Key,
						count = g.Count()
					}).OrderBy(c => c.key);

				var list = results.ToList();
				
				scope.Complete();
			}
		}

		[Test]
		public void Test_GroupBy_AggregateCount_With_Variable_Condition()
		{
			var x = 0;

			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
							  where student.School.Id == x
							  group student by student.Firstname
					into g
							  select new
							  {
								  key = g.Key,
								  count = g.Count()
							  };

				var list = results.ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_GroupBy_AggregateCount_With_OrderBy()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
							  group student by student.Firstname
								  into g
								  orderby g.Count() descending
								  select new
								  {
									  key = g.Key
								  };

				results.ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Implicit_Join()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from
						student in this.model.Students
					where
						student.Sex == Sex.Male && student.School.Name.EndsWith("School")
					select
						new
						{
							student.School,
							MaleStudents = student
						};

				var z = query.ToList();

				Assert.IsFalse(query.ToList().First().School.IsDeflatedReference());

				var studentsBySchool = query.ToLookup(x => x.School, x => x.MaleStudents);

				Assert.That(studentsBySchool.Count, Is.EqualTo(2));

				foreach (var schoolStudents in studentsBySchool)
				{
					foreach (var student in schoolStudents)
					{
						Assert.That(student.Sex, Is.EqualTo(Sex.Male));
					}
				}
			}
		}

		[Test]
		public void Test_Implicit_Join_2()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from
						student in this.model.Students
					where
						student.Sex == Sex.Female 
					select
						student.IncludeDirect(c => c.BestFriend.Address);

				var count = 0;

				foreach (var student in query.ToList())
				{
					if (student.BestFriend == null)
					{
						continue;
					}

					Assert.IsFalse(student.BestFriend.IsDeflatedReference());

					if (student.BestFriend.Address == null)
					{
						continue;
					}

					Assert.IsFalse(student.BestFriend.Address.IsDeflatedReference());

					count++;
				}

				Assert.That(count, Is.GreaterThan(0));
			}
		}

		[Test]
		public void Test_Implicit_Join_3()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from
						student in this.model.Students
					where
						student.Sex == Sex.Male &&
						student.School.Name.EndsWith("School")
					select
						student;

				foreach (var student in query.ToList())
				{
					Assert.IsTrue(student.School.IsDeflatedReference());
				}

				var firstStudent =
					(from
						student in this.model.Students
						where
							student.Firstname == "Tum"
							&& student.Sex == Sex.Male
							&& student.School.Name.EndsWith("School")
						select
							student.IncludeDirect(c => c.Address)).First();

				Assert.AreEqual("Bruce's Kung Fu School", firstStudent.School.Name);
				Assert.IsFalse(firstStudent.IsDeflatedReference());
				Assert.AreEqual(178, firstStudent.Address.Number);
			}
		}

		[Test]
		public void Test_Select_Any_1()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.FirstOrDefault();
				var result = this.model.Students.Where(c => school.Students.Where(d => d.Id == c.Id).Any()).ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Select_Any_2()
		{
			using (var scope = NewTransactionScope())
			{
				var a = this.model.Schools.FirstOrDefault();
				var school = this.model.Schools.GetReference(a.Id);
				var result = this.model.Students.Where(c => school.Students.Any(d => d.Id == c.Id)).ToList();

				Assert.IsTrue(result.All(c => c.School == school));

				scope.Complete();
			}
		}

		[Test]
		[Category("IgnoreOnMono")]
		/* Currently fails with Mono */
		public void Test_GroupBy_Project_Group0()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
							  group student by student.Sex
					into g
							  select new { sex = g.Key, g };

				var students = this.model.Students.ToList();
				var s2 = from student in students
						 group student by student.Sex
					into g
						 select new { sex = g.Key, c = g };

				var s3 = results.ToList();

				foreach (var value in results)
				{
					var xx = value.g.ToList();

					Console.WriteLine(value);
				}
			}
		}

		[Test]
		[Category("IgnoreOnMono")]
		/* Currently fails with Mono */
		public void Test_GroupBy_Project_Group1()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
					group student by student.Sex
					into g
					select new { sex = g.Key , date = g.Max(c => c.SpecialDate), count = g.Count(), g };

				var students = this.model.Students.ToList();
				var s2 = from student in students
						 group student by student.Sex
					into g
						 select new { sex = g.Key, date = g.Max(c => c.SpecialDate), count = g.Count(), g };

				var s3 = results.ToList();

				foreach (var value in results)
				{
					var xx = value.g.ToList();

					Console.WriteLine(value);
				}
			}
		}

		[Test]
		public void Test_GroupBy_Project_Group2()
		{
			using (var scope = NewTransactionScope())
			{
				var results = (from student in this.model.Students
					group student by student.Nickname
					into g
					select g).Select(c => c.Key);

				var list = results.ToList();

				foreach (var item in list)
				{
					Console.WriteLine(item);
				}
			}
		}

		[Test]
		public void Test_GroupBy_Project_Group2a()
		{
			using (var scope = NewTransactionScope())
			{
				var results = this.model.Students.GroupBy(c => (long?)c.Address.Id, c => c.Id).OrderBy(c => c.Key);

				var list = results.ToList();
				var list2 = this.model.Students.ToList().GroupBy(c => c.Address?.Id, c => c.Id).OrderBy(c => c.Key).ToList();

				var i = list.SelectMany(c => c).Count();

				Assert.That(i != 0);

				var left = list.SelectMany(c => c).OrderBy(c => c).ToList();
				var right = list2.SelectMany(c => c).OrderBy(c => c).ToList();

				Assert.That(left.SequenceEqual(right));
			}
		}

		[Test]
		public void Test_GroupBy_Project_Group3()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
							  group student by student.Firstname
								  into g
								  select g;

				var list = results.ToList();

				foreach (var item in list)
				{
					Console.WriteLine(item.Key);
					Console.WriteLine(item.Count());
				}
			}
		}

		[Test]
		public void Test_GroupBy_Project_Group4()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
					group student by new {student.Firstname, student.Lastname}
					into g
					select g;

				var list = results.ToList();

				foreach (var item in list)
				{
					Console.WriteLine(item.Key);
					Console.WriteLine(item.Count());
				}
			}
		}

		[Test]
		[Category("IgnoreOnMono")]
		/* Currently fails with Mono */
		public void Test_GroupBy_Project_Group5()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
					group student by new {student.Firstname, student.Lastname}
					into g
					orderby g.Key
					select new {g, first = g.FirstOrDefault(c => c.Firstname != "")};

				var list = results.ToList().SelectMany(c => c.g).OrderBy(c => c.Id).ToList();
				
				var results2 = from student in this.model.Students.ToList().OrderBy(c => c.Nickname)
							   group student by new { student.Firstname, student.Lastname }
					into g
					orderby g.Key.Firstname
							   select new { g, first = g.FirstOrDefault(c => c.Firstname != "") };

				var list2 = results2.SelectMany(c => c.g).OrderBy(c => c.Id).ToList();

				Assert.That(list.SequenceEqual(list2));
			}
		}

		[Test]
		public void Test_GroupBy_Project_Group6()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
							  join school in this.model.Schools on student.School equals school
					group student by new {student, school}
					into g
					select g;

				var list = results.ToList();

				foreach (var item in list)
				{
					Console.WriteLine(item);
				}
			}
		}

		[Test]
		public void Test_GroupBy_Project_Group7()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
					join school in this.model.Schools on student.School equals school
					group student by new {student, school}
					into g
					orderby g.Key.student.Nickname, g.Key.school.Name
					select g;

				var list = results.ToList();

				foreach (var item in list)
				{
					Console.WriteLine(item);
				}
			}
		}

		[Test]
		public void Test_GroupBy_Project_Group8()
		{
			using (var scope = NewTransactionScope())
			{
				var results = from student in this.model.Students
							  join school in this.model.Schools on student.School equals school
							  group student by new { student, school }
								  into g
								  orderby g.Key.student.Nickname, g.Count()
								  select g;

				var list = results.ToList();

				foreach (var item in list)
				{
					Console.WriteLine(item);
				}
			}
		}

		private void Test_Check_Null<T>(Expression<Func<Student, T>> grouping)
		{
			using (var scope = NewTransactionScope())
			{
				var results = this.model.Students.GroupBy(grouping)
					.Where(c => c.Key != null);

				var list = results.ToList();

				foreach (var item in list)
				{
					Console.WriteLine(item);
				}
			}
		}

		[Test]
		public void Test_Check_Grouping_Expression_From_Generic_Method()
		{
			Test_Check_Null(c => c.SexOptional);
		}

		[Test]
		public void Test_OrderBy_Then_Count()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.OrderBy(c => (string)(object)c.Nickname).Count();
			}
		}

		[Test]
		public void Test_OrderBy_Then_Skip_Then_Count()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.OrderBy(c => c.Nickname).Skip(1).Count();
			}
		}

		[Test]
		public void Test_OrderBy_With_Implicit_Join()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.OrderBy(c => c.BestFriend.BestFriend.BestFriend.Address.Country).ToList();
			}
		}

		[Test]
		public void Test_Implicit_Join_With_String_Length()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Count(c => c.BestFriend.Address.Country.Length == 0);
			}
		}

		[Test]
		public void Test_Implicit_Join_With_Aggregate_On_String_Length()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Max(c => c.BestFriend.Height);
			}
		}

		[Test]
		public void Test_Aggregate_With_Implicit_Join()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Max(c => c.BestFriend.Address.Street).ToList();
			}

			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Min(c => c.BestFriend.Address.Street).ToList();
			}

			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Sum(c => c.BestFriend.BestFriend.BestFriend.Address.Number);
			}

			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Average(c => c.BestFriend.Address.Number);
			}

			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Count(c => c.BestFriend.Address.Number == 0);
			}
		}

		[Test]
		public void Test_Count_With_Implicit_Join()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Count(c => c.BestFriend.Address.Country == "");
			}
		}

		[Test]
		public void Test_OrderBy_Then_Skip_Then_Take_Then_Count()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.OrderBy(c => c.Nickname).Skip(1).Take(10).Count();
			}
		}

		[Test]
		public void Test_Skip_Only()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Skip(1).ToList();
			}
		}

		[Test]
		public void Test_Skip_Take1()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model
					.Students
					.Include(c => c.School)
					.Skip(0)
					.Take(5)
					.OrderBy(c => c.Nickname)
					.ToList();
			}
		}

		[Test]
		public void Test_Skip_Take2()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model
					.Students
					.Include(c => c.School)
					.Skip(0)
					.Take(5)
					.OrderBy(c => c.School.Name)
					.ToList();
			}
		}

		[Test]
		public void Test_Skip_Take3()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model
					.Students
					.Include(c => c.School)
					.Skip(0)
					.Take(5)
					.ToList();
			}
		}

		[Test]
		public void Test_Distinct_With_Count()
		{
			using (var scope = NewTransactionScope())
			{
				var result1 = this.model.Students.Distinct();
				var result2 = this.model.Students.Distinct().Count();
			}
		}

		[Test]
		public void Test_Skip_With_Distinct()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Select(c => new { c.Lastname }).Distinct().Skip(1).ToList();
				var result2 = this.model.Students.ToList().Select(c => new { c.Lastname }).Distinct().Skip(1).ToList();
			}
		}

		[Test]
		public void Test_Take_With_Distinct()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Select(c => new { c.Lastname }).Distinct().Take(1).ToList();
				var result2 = this.model.Students.ToList().Select(c => new { c.Lastname }).Distinct().Take(1).ToList();
			}
		}

		[Test]
		public void Test_Skip_With_OrderBy()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.OrderBy(c => c.Nickname).ThenBy(c => c.Id).Skip(1).ToList();
				var result2 = this.model.Students.ToList().OrderBy(c => c.Nickname).ThenBy(c => c.Id).Skip(1).ToList();

				Assert.IsTrue(result.SequenceEqual(result2));
			}
		}

		[Test]
		public void Test_Skip_With_Distinct_OrderBy()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Select(c => c.Nickname).Distinct().OrderBy(c => c).Skip(1).ToList();
				var result2 = this.model.Students.ToList().Select(c => c.Nickname).Distinct().OrderBy(c => c).Skip(1).ToList();

				Assert.IsTrue(result.SequenceEqual(result2));
			}
		}

		[Test]
		public void Test_Select_Multiple_Nested_OrderBySkipDistinct()
		{
			using (var scope = NewTransactionScope())
			{
				var s1 = this.model.Students
					.Select(c => c.Nickname)
					.OrderBy(c => c)
					.Distinct()
					.Skip(1)
					.Distinct()
					.Skip(2)
					.ToList();

				var s2 = this.model.Students.ToList()
					.Select(c => c.Nickname)
					.OrderBy(c => c)
					.Distinct()
					.Skip(1)
					.Distinct()
					.Skip(2)
					.ToList();

				Assert.IsTrue(s1.SequenceEqual(s2));
			}
		}

		[Test]
		public void Test_SelectMany()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students;

				var query = from student in students
					from school in this.model.Schools
					where student.School.Name == school.Name
					select new { student, school };

				var results = query.ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Contains_On_DAOs()
		{
			using (var scope = NewTransactionScope())
			{
				var student = this.model.Students.First();

				var result = this.model.Students.Contains(student);
			}
		}

		[Test]
		public void Test_List_Contains_Inside_Query()
		{
			using (var scope = NewTransactionScope())
			{
				var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

				var student = this.model.Students.Select(c => c.Id).Where(c => ids.Contains(c)).ToList();
				
			}
		}

		[Test]
		public void Test_Any_On_DAOs()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model.Students.Any();
			}
		}

		[Test]
		public void Test_Any_On_DAOs_With_Predicate()
		{
			Func<Task> func = async delegate
			{
				using (var scope = DataAccessScope.CreateReadCommitted())
				{
					var result1 = await this.model.Students.AnyAsync(c => c.Email != null);
					var result2 = this.model.Students.ToList().Any(c => c.Email != null);

					Assert.AreEqual(result2, result1);
				}
			};

			func().GetAwaiter().GetResult();
		}

		[Test]
		public void Test_All_On_DAOs_With_Predicate()
		{
			Func<Task> func = async delegate
			{
				using (var scope = DataAccessScope.CreateReadCommitted())
				{
					var result1 = await this.model.Students.AllAsync(c => c.Email == null);
					var result2 = this.model.Students.ToList().All(c => c.Email == null);

					Assert.AreEqual(result2, result1);
				}
			};

			func().GetAwaiter().GetResult();
		}

		[Test]
		public void Test_All_On_DAOs_With_Predicate2()
		{
			using (var scope = NewTransactionScope())
			{
				var result1 = this.model.Students.All(c => c.Firstname != null && c.Firstname != "George");
				var result2 = this.model.Students.ToList().All(c => c.Firstname != null && c.Firstname != "George");

				Assert.AreEqual(result2, result1);
			}
		}

		[Test]
		public void Test_StringIsNullOrEmpty()
		{
			using (var scope = NewTransactionScope())
			{
				var result = this.model
					.Students
					.Where(c => string.IsNullOrEmpty(c.Firstname))
					.ToList();
			}
		}

		[Test]
		public void Test_JoinWithAnonymousTypeCondition()
		{
			using (var scope = NewTransactionScope())
			{
				var query =
					from school in this.model.Schools
					join student in this.model.Students on new { SchoolId = school.Id, SchoolName = school.Name } equals new { SchoolId = student.School.Id, SchoolName = student.School.Name }
					select new Tuple<School, Student>(school, student);

				var results = query.ToList();
			}
		}

		[Test]
		public void Test_Bool()
		{
			var id = Guid.Empty;

			try
			{
				using (var scope = NewTransactionScope())
				{
					var student = this.model.Students.Create();

					student.School = this.model.Schools.Single(c => c.Name == "Bruce's Kung Fu School");

					student.Overseas = true;

					scope.Flush();

					id = student.Id;

					scope.Complete();
				}

				using (var scope = NewTransactionScope())
				{
					var student = this.model.Students.SingleOrDefault(c => c.Id == id && c.Overseas);

					Assert.IsNotNull(student);

					scope.Complete();
				}
			}
			finally
			{
				using (var scope = NewTransactionScope())
				{
					var student = this.model.Students.SingleOrDefault(c => c.Id == id && c.Overseas);

					student.Delete();

					scope.Complete();
				}
			}
		}

		[Test]
		public void Test_Rollback_DataAccessScope_Inside_TransactionScope()
		{
			using (var scope = new TransactionScope())
			{
				using (var scope2 = new TransactionScope())
				{
					using (var dataAccessScope = DataAccessScope.CreateReadCommitted())
					{
						this.model.Students.SingleOrDefault(c => c.Id == null);
					}
				}
			}
		}

		[Test]
		public void Test_SelectForUpdate()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model
					.Students
					.OrderBy(c => c.Nickname)
					.Select(c => c.Id == Guid.Empty)
					.ForUpdate()
					.ToList();
			}
		}

		[Test]
		public void Test_Select_ForUpdate_First()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model
					.Students
					.OrderBy(c => c.Nickname)
					.Select(c => c.Id == Guid.Empty)
					.ForUpdate()
					.FirstOrDefault();
			}
		}

		[Test]
		public void Test_Concat()
		{
			using (var scope = NewTransactionScope())
			{
				var results = this.model.Students.Concat(this.model.Students).ToList();
			}
		}

		[Test]
		public void Test_Union()
		{
			using (var scope = NewTransactionScope())
			{
				var results = this.model.Students.Union(this.model.Students).ToList();
			}
		}

		[Test]
		public void Test_NullCheck_In_Projector()
		{
			using (var scope = NewTransactionScope())
			{
				var x = from cat in this.model.Cats
					let name = cat != null ? cat.Name : ""
					where name == ""
					select new { cat, name };

				var results = x.ToList();
			}
		}


		[Test]
		public void Test_Compare_Strings()
		{
			using (var scope = NewTransactionScope())
			{
				var x = from student1 in this.model.Students
					from student2 in this.model.Students
					where student1.Nickname.CompareTo(student2.Nickname) > 0
					select new { student1, student2 };

				var results = x.ToList();
			}
		}

		[Test]
		public void Test_Compare_Guids()
		{
			using (var scope = NewTransactionScope())
			{
				var x = from student1 in this.model.Students
						from student2 in this.model.Students
						where student1.Id.CompareTo(student2.Id) > 0
						select new { student1, student2 };

				var results = x.ToList();
			}
		}

		[Test]
		public void Test_Binary_Expression_With_Bools()
		{
			using (var scope = NewTransactionScope())
			{
				var x = from student in this.model.Students
					where student.Lastname.ToLower() == "TEST".ToLower() && student.Firstname != "" && student.Overseas
					select student;

				var results = x.ToList();
			}
		}

		[Test]
		public void Test_QuerySimilarStructure_Different_Variables_And_Consts()
		{
			using (var scope = NewTransactionScope())
			{
				var s = "Mars";
				
				var x = from cat in this.model.Cats
						let name = cat != null ? cat.Name : ""
						where name == s
						select new { cat, name };

				Assert.AreEqual(1, x.ToList().Count);

				var y = from cat in this.model.Cats
					let name = cat != null ? cat.Name : ""
					where name == "hello"
					select new { cat, name };

				Assert.AreEqual(0, y.ToList().Count);

				var z = from cat in this.model.Cats
						let name = cat != null ? cat.Name : ""
						where name == s
						select new { cat, name };

				Assert.AreEqual(1, z.ToList().Count);

				s = "test";

				Assert.AreEqual(0, x.ToList().Count); 
			}
		}

		[Test]
		public void Test_Get_Related_Similar_Query_Structure()
		{
			long school1Id;
			long school2Id;

			using (var scope = NewTransactionScope())
			{
				var school1 = this.model.Schools.Create();
				var school2 = this.model.Schools.Create();

				var student1 = school1.Students.Create();
				student1.Firstname = "Update1";
				student1.Lastname = "Last";

				var student2 = school2.Students.Create();
				student2.Firstname = "Update1";
				student2.Lastname = "Last";

				scope.Flush();

				school1Id = school1.Id;
				school2Id = school2.Id;


				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				var obj1 = this.model.Schools.GetByPrimaryKey(school1Id);
				var obj2 = this.model.Schools.GetByPrimaryKey(school2Id);

				var students1 = obj1.Students.ToList();
				var students2 = obj2.Students.ToList();

				Assert.IsTrue(students1.All(c => c.School.Id == school1Id));
				Assert.IsTrue(students2.All(c => c.School.Id == school2Id));

				this.model.Students.Delete(c => c.School.Id == school1Id || c.School.Id == school2Id);
				this.model.Schools.Delete(c => c.Id == school1Id || c.Id == school2Id);

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_With_Skip_Take_OrderBy()
		{
			using (var scope = NewTransactionScope())
			{
				var students = this.model.Students.ToList();

				var results = this.model.Students.Skip(0).Take(100).Where(c => c.FavouriteNumber > 10).OrderByDescending(c => c.Nickname).ToList();
				var results2 = this.model.Students.ToList().Skip(0).Take(100).Where(c => c.FavouriteNumber > 10).OrderByDescending(c => c.Nickname).ToList();

				Assert.IsTrue(results.SequenceEqual(results2));
			}
		}

		[Test]
		public void Test_FastCompiler()
		{
			using (var scope = new TransactionScope())
			{
				var student1 = this.model.Students.Skip(0).First();
				var student2 = this.model.Students.Skip(1).First();

				var s1 = this.model.Students.First(c => c == student1);
				var s2 = this.model.Students.First(c => c == student2);
			}
		}

		void Foo(string[] names)
		{
			using (var scope = new TransactionScope())
			{
				for (var i = 0; i < 2; i++)
				{
					var student1 = this.model
						.Students
						.Where(c => names.Contains(c.Address.Country))
						.Select(c => new { X = c.Firstname, Y = c.Lastname })
						.ToList();
				}
			}
		}

		[Test]
		public void Test_FastCompiler2()
		{
			Foo(new[] { "a", "b" });
			Foo(new[] { "c", "d" });
		}

		public struct TestStruct
		{
			public long X;
		}

		[Test]
		public void Test_DataAccessObjectAwareComparer()
		{
			using (var scope = new TransactionScope())
			{
				var students1 = this.model
					.Students.Select(c => new TestStruct { X = c.SerialNumber1 })
					.Union(this.model.Students.Select(c => new TestStruct { X = c.SerialNumber1 }))
					.ToList()
					.OrderBy(c => c.X);

				var students2 = this.model
					.Students.ToList().Select(c => new TestStruct { X = c.SerialNumber1 })
					.Union(this.model.Students.ToList().Select(c => new TestStruct { X = c.SerialNumber1 }))
					.ToList()
					.OrderBy(c => c.X);

				Assert.IsTrue(students1.SequenceEqual(students2));
			}
		}

		[Test]
		public void Test_DataAccessObjectAwareComparer2()
		{
			using (var scope = new TransactionScope())
			{
				var students1 = this.model
					.Students.Select(c => (TestStruct?)new TestStruct { X = c.SerialNumber1 })
					.Union(this.model.Students.Select(c => (TestStruct?)new TestStruct { X = c.SerialNumber1 }))
					.ToList()
					.OrderBy(c => c.Value.X);

				var students2 = this.model
					.Students.ToList().Select(c => (TestStruct?)new TestStruct { X = c.SerialNumber1 })
					.Union(this.model.Students.ToList().Select(c => (TestStruct?)new TestStruct { X = c.SerialNumber1 }))
					.ToList()
					.OrderBy(c => c.Value.X);

				Assert.IsTrue(students1.SequenceEqual(students2));
			}
		}
		
		private void QuerySchoolAndStudentsLookup(IEnumerable<string> schoolNames)
		{
			var query =
				from school in this.model.Schools
				join st in this.model.Students on school equals st.School into g
				from student in g.DefaultIfEmpty()
				select new
				{
					School = school,
					Student = student
				};

			if (schoolNames != null && schoolNames.Any())
			{
				query = query.Where(x => schoolNames.Contains(x.School.Name));
			}

			var scopes = query.ToLookup(x => x.School, x => x.Student);
			var result = scopes.ToList();
		}

		[Test]
		public void Test_QuerySchoolAndStudentsLookup()
		{
			for (var i = 0; i < 10; i++)
			{
				QuerySchoolAndStudentsLookup(new [] { "Bruce's Kung Fu School" });
			}
		}

		[Test]
		public void Test_AutoIncrementValidation()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "School" + MethodBase.GetCurrentMethod().Name;
				
				var student = school.Students.Create();

				scope.Flush();
				Console.WriteLine($"serialnumber1: {student.SerialNumber1}");
				Assert.IsTrue(student.SerialNumber1 % 2 == 0);
				
				student = school.Students.Create();
				scope.Flush();
				Console.WriteLine($"serialnumber1: {student.SerialNumber1}");
				Assert.IsTrue(student.SerialNumber1 % 2 == 0);

				student = school.Students.Create();
				scope.Flush();
				Console.WriteLine($"serialnumber1: {student.SerialNumber1}");
				Assert.IsTrue(student.SerialNumber1 % 2 == 0);

				student = school.Students.Create();
				scope.Flush();
				Console.WriteLine($"serialnumber1: {student.SerialNumber1}");
				Assert.IsTrue(student.SerialNumber1 % 2 == 0);

				student = school.Students.Create();
				scope.Flush();
				Console.WriteLine($"serialnumber1: {student.SerialNumber1}");
				Assert.IsTrue(student.SerialNumber1 % 2 == 0);

				student = school.Students.Create();
				scope.Flush();
				Console.WriteLine($"serialnumber1: {student.SerialNumber1}");
				Assert.IsTrue(student.SerialNumber1 % 2 == 0);

				scope.Complete();
			}
		}
	}
}
