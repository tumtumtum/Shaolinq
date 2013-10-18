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
			this.CreateObjects();
		}

		private void CreateObjects()
		{
			using (var scope = new TransactionScope())
			{
				var school = model.Schools.NewDataAccessObject();
				
				school.Name = "Bruce's Kung Fu School";

				var tum = school.Students.NewDataAccessObject();

				var address = this.model.Address.NewDataAccessObject();
				address.Number = 178;
				address.Street = "Fake Street";

				tum.Firstname = "Tum";
				tum.Lastname = "Nguyen";
				tum.Sex = Sex.Male;
				tum.Address = address;
				tum.Height = 177;
				tum.FavouriteNumber = 36;
				tum.Address = address;
				tum.Birthdate = new DateTime(1979, 12, 24, 04, 00, 00);

				var mars = school.Students.NewDataAccessObject();

				mars.Firstname = "Mars";
				mars.Lastname = "Nguyen";
				mars.Nickname = "The Cat";
				mars.Height = 20;
				mars.Address = address;
				mars.Sex = Sex.Female;
				mars.BestFriend = tum;
				mars.Birthdate = new DateTime(2003, 11, 2);
				mars.FavouriteNumber = 1;

				school = model.Schools.NewDataAccessObject();

				school.Name = "Brandon's Kung Fu School";

				var chuck1 = school.Students.NewDataAccessObject();

				var address2 = this.model.Address.NewDataAccessObject();
				address2.Number = 1799;
				address2.Street = "Fake Street";

				chuck1.Firstname = "Chuck";
				chuck1.Lastname = "Norris";
				chuck1.Nickname = "God";
				chuck1.Address = address2;
				chuck1.Height = Double.PositiveInfinity;
				chuck1.FavouriteNumber = 8;

				var chuck2 = school.Students.NewDataAccessObject();

				chuck2.Firstname = "Chuck";
				chuck2.Lastname = "Yeager";
				chuck2.Height = 182;
			
				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_GroupBy_With_Count()
		{
			using (var scope = new TransactionScope())
			{
				var results = from student in this.model.Students
							  group student by student.Firstname into g
							  select new { Count = g.Count(), Name = g.Key };

				var resultsArray = results.OrderBy(c => c.Name).ToArray();

				Assert.AreEqual("Chuck", resultsArray[0].Name);
				Assert.AreEqual(2, resultsArray[0].Count);
				
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
			using (var scope = new TransactionScope())
			{
				var results = from student in this.model.Students
							  where student.Address == new TempObject(model).GetAddress() 
							  orderby  student.Firstname
							  select new { student };

				var array =  results.ToArray();

				Assert.AreEqual(2, array.Length);
				Assert.AreEqual("Mars", array[0].student.Firstname);
				Assert.AreEqual("Tum", array[1].student.Firstname);

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_With_Multiple_From_Manual_Join()
		{
			using (var scope = new TransactionScope())
			{
				var results = from student in this.model.Students
							  from school in this.model.Schools
							  orderby  student.Firstname
							  where student.School.Id == school.Id
							  select new { student.Fullname, school.Name };

				var students = this.model.Students.ToArray();
				var schools = this.model.Schools.ToArray();

				var resultsLocal = from student in students
							  from school in schools
							  orderby student.Firstname
							  where student.School.Id == school.Id
							  select new { student.Fullname, school.Name };

				Assert.IsTrue(resultsLocal.SequenceEqual(results));

				scope.Complete();
			}
		}

		[Test]
		public virtual void Test_Query_With_Skip()
		{
			using (var scope = new TransactionScope())
			{
				var students = this.model.Students.ToList();

				var results = ((from student in this.model.Students
				                orderby student.Firstname
				                select student).Skip(2)).ToArray();

				Assert.IsTrue(students.OrderBy(c => c.Firstname).Skip(2).SequenceEqual(results));
			}
		}

		[Test]
		public virtual void Test_Query_With_Take()
		{
			using (var scope = new TransactionScope())
			{
				var students = this.model.Students.ToList();

				var results = ((from student in this.model.Students
				                orderby student.Firstname
				                select student).Take(2));

				Assert.IsTrue(students.OrderBy(c => c.Firstname).Take(2).SequenceEqual(results));
			}
		}

		[Test]
		public virtual void Test_Query_With_Skip_Take()
		{
			using (var scope = new TransactionScope())
			{
				var students = this.model.Students.ToList();

				var results = ((from student in this.model.Students
				                orderby student.Firstname
				                select student).Skip(1).Take(2)).ToList();

				Assert.AreEqual(results.Count, 2);

				Assert.IsTrue(results.SequenceEqual(students.OrderBy(c => c.Firstname).Skip(1).Take(2)));
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

			using (var scope = new TransactionScope())
			{
				var count = this.model.Students.Count(c => list.Contains(c.Sex));

				Assert.That(count, Is.GreaterThan(0));
			}
		}

		[Test]
		public virtual void Test_Get_Advanced_Computed_Property_With_AutoIncrement_Guid()
		{
			using (var scope = new TransactionScope())
			{
				var tum = model.Students.FirstOrDefault(c => c.Firstname == "Tum");
				var student = model.Students.FirstOrDefault(c => c.Urn == "urn:student:" + tum.Id.ToString("N"));

				Assert.AreSame(tum, student);
			}
		}

		[Test]
		public virtual void Test_Get_Advanced_Computed_Property_With_AutoIncrement_Long()
		{
			using (var scope = new TransactionScope())
			{
				var school1 = model.Schools.First();
				var school2 = model.Schools.First(c => c.Urn == "urn:school:" + school1.Id);

				Assert.AreSame(school1, school2);
			}
		}

		[Test]
		public virtual void Test_Select_With_Ternary_Operator()
		{
			using (var scope = new TransactionScope())
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
		public virtual void Test_Select_With_Scope()
		{
			using (var scope = new TransactionScope())
			{
				var student1 = model.Students.Single(c => c.Firstname == "Tum");
				var student2 = model.Students.Single(c => c.Firstname == "Tum");

				Assert.AreSame(student1, student2);
			}
		}

		[Test]
		public virtual void Test_Select_Without_Scope()
		{
			var student1 = model.Students.Single(c => c.Firstname == "Tum");
			var student2 = model.Students.Single(c => c.Firstname == "Tum");

			Assert.AreNotSame(student1, student2);
		}

		[Test]
		public void Test_Query_Related_Objects1()
		{
			using (var scope = new TransactionScope())
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
			using (var scope = new TransactionScope())
			{
				var studentCountBySchoolId = this.model.Schools.ToList().ToDictionary(c => c.Id, c => c.Students.Count());

				foreach (var school in this.model.Schools)
				{
					var expected = studentCountBySchoolId[school.Id];

					Assert.AreEqual(expected, school.Students.Count());
					Assert.AreEqual(expected, this.model.Students.Count(c => c.School == school));
				}
			}
		}

		[Test]
		public void Test_Query_First1()
		{
			var student = model.Students.First();
		}

		[Test]
		public void Test_Query_Check_Has_Changed()
		{
			var student = model.Students.First();

			Assert.IsFalse(((IDataAccessObject)student).HasObjectChanged);
		}

		[Test]
		public void Test_Query_First2()
		{
			using (var scope = new TransactionScope())
			{
				var student1 = model.Students.Where(c => c.Firstname == "Tum").First();
				var student2 = this.model.Students.First(c => c.Firstname == "Tum");

				Assert.AreSame(student1, student2);

				scope.Complete();
			}
		}

		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void Test_Query_First3()
		{
			using (var scope = new TransactionScope())
			{
				var student1 = this.model.Students.First(c => c.Firstname == "iewiorueo");

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_FirstOrDefault1()
		{
			using (var scope = new TransactionScope())
			{
				var student1 = this.model.Students.FirstOrDefault(c => c.Firstname == "iewiorueo");

				Assert.IsNull(student1);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_FirstOrDefault2()
		{
			using (var scope = new TransactionScope())
			{
				var student1 = this.model.Students.FirstOrDefault(c => c.Firstname == "Tum");

				Assert.IsNotNull(student1);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_With_Where1()
		{
			using (var scope = new TransactionScope())
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
			using (var scope = new TransactionScope())
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
			using (var scope = new TransactionScope())
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
			using (var scope = new TransactionScope())
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
			using (var scope = new TransactionScope())
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
			using (var scope = new TransactionScope())
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
			using (var scope = new TransactionScope())
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
			using (var scope = new TransactionScope())
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
			using (var scope = new TransactionScope())
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
			using (var scope = new TransactionScope())
			{
				var product1 = this.model.Products.NewDataAccessObject();

				product1.Name = "Uniform";
				product1.Price = 150;

				var product2 = this.model.Products.NewDataAccessObject();

				product2.Name = "Belt";
				product2.Price = 22;

				var product3 = this.model.Products.NewDataAccessObject();

				product3.Name = "Belt";
				product3.Price = 56;

				scope.Flush(model);

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
			using (var scope = new TransactionScope())
			{
				var totalHeight = model.Students.Sum(c => c.Height);

				Assert.That(totalHeight, Is.GreaterThanOrEqualTo(177));
			}
		}

		[Test]
		public void Test_Query_Aggregate_Sum2()
		{
			using (var scope = new TransactionScope())
			{
				var totalHeight = model.Students.Where(c => c.Fullname == "Tum Nguyen").Sum(c => c.Height);

				Assert.AreEqual(177, totalHeight);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Sum3()
		{
			using (var scope = new TransactionScope())
			{
				var serverSideResult = model.Students.Sum(c => c.Height);
				var allStudents = model.Students.ToList();
				var clientSideResult = allStudents.Sum(c => c.Height);

				Assert.That(serverSideResult, Is.GreaterThanOrEqualTo(197));
				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Average()
		{
			using (var scope = new TransactionScope())
			{
				var serverSideResult = model.Students.Average(c => c.Height);
				var allStudents = model.Students.ToList();
				var clientSideResult = allStudents.Average(c => c.Height);

				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Average_Complex1()
		{
			using (var scope = new TransactionScope())
			{
				var serverSideResult = model.Students.Average(c => c.Height * 2);
				var allStudents = model.Students.ToList();
				var clientSideResult = allStudents.Average(c => c.Height * 2);

				Assert.AreEqual(clientSideResult, serverSideResult);
				Assert.AreEqual(allStudents.Average(c => c.Height) * 2, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Average_Complex2()
		{
			using (var scope = new TransactionScope())
			{
				var serverSideResult = model.Students.Average(c => c.Height + c.FavouriteNumber);
				var allStudents = model.Students.ToList();
				var clientSideResult = allStudents.Average(c => c.Height + c.FavouriteNumber);

				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Max()
		{
			using (var scope = new TransactionScope())
			{
				var serverSideResult = model.Students.Max(c => c.Height);
				var allStudents = model.Students.ToList();
				var clientSideResult = allStudents.Max(c => c.Height);

				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Min()
		{
			using (var scope = new TransactionScope())
			{
				var serverSideResult = model.Students.Min(c => c.Height);
				var allStudents = model.Students.ToList();
				var clientSideResult = allStudents.Min(c => c.Height);

				Assert.AreEqual(clientSideResult, serverSideResult);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Sum_With_Complex_Aggregate_Computation()
		{
			using (var scope = new TransactionScope())
			{
				var totalHeight = model.Students.Where(c => c.Fullname == "Tum Nguyen").Sum(c => c.Height * 2);

				Assert.AreEqual(354, totalHeight);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Sum_With_Group1()
		{
			var sum = (from student in this.model.Students
					   where student.Firstname == "Tum"
						group student by student.Id
									into g
									select g.Sum(x => x.Height)).FirstOrDefault();

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
		}

		[Test]
		public virtual void Test_Coalesce_Function()
		{
			using (var scope = new TransactionScope())
			{
				var tum = (from student in model.Students
				           where student.Nickname == ""
				                 && student.Firstname == "Tum"
				           select new
				           {
					           Student = student
				           }).FirstOrDefault();

				Assert.IsNull(tum);
			}

			using (var scope = new TransactionScope())
			{
				var tum = (from student in model.Students
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
		public virtual void Test_GroupBy_Date()
		{
			using (var scope = new TransactionScope())
			{
				var tum2 = model.Schools.First(c => c.Name.Contains("Bruce")).Students.NewDataAccessObject();

				tum2.Firstname = "Tum";
				tum2.Lastname = "Nguyen";
				tum2.Height = 177;
				tum2.FavouriteNumber = 36;
				tum2.Birthdate = new DateTime(1979, 12, 24, 05, 00, 00);

				scope.Flush(model);

				var group = (from student in model.Students
							 group student by student.Birthdate
								 into g
								 select new
								 {
									 Date = g.Key,
									 Count = g.Count()
								 }).ToList();

				Assert.That(group.Count, Is.GreaterThan(2));

				group = (from student in model.Students
						 where student.Firstname == "Tum"
						 group student by student.Birthdate
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
		}

		[Test]
		public virtual void Test_GroupBy_Date_With_Date_Only()
		{
			using (var scope = new TransactionScope())
			{
				var tum2 = model.Schools.First(c => c.Name.Contains("Bruce")).Students.NewDataAccessObject();

				tum2.Firstname = "Tum";
				tum2.Lastname = "Nguyen";
				tum2.Height = 177;
				tum2.FavouriteNumber = 36;
				tum2.Birthdate = new DateTime(1979, 12, 24, 05, 00, 00);

				scope.Flush(model);

				var group = (from student in model.Students
				             group student by student.Birthdate.GetValueOrDefault().Date
				             into g
				             select new
				             {
					             Date = g.Key,
					             Count = g.Count()
				             }).ToList();

				Assert.That(group.Count, Is.GreaterThan(2));

				group = (from student in model.Students
				         where student.Firstname == "Tum"
				         group student by student.Birthdate.GetValueOrDefault().Date
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
			using (var scope = new TransactionScope())
			{
				Assert.AreEqual(1, this.model.Students.Count(c => c.Firstname == "Tum"));
			}
		}
	}
}
