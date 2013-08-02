using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

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
				
				school.Name = "School";

				var student = school.Students.NewDataAccessObject();

				student.Firstname = "Tum";
				student.Lastname = "Nguyen";
				student.Height = 177;

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_First1()
		{
			var student = model.Students.First();
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
		public void Test_Query_Aggregate_Sum_With_Complex_Aggregate_Computation()
		{
			using (var scope = new TransactionScope())
			{
				var totalHeight = model.Students.Where(c => c.Fullname == "Tum Nguyen").Sum(c => c.Height * 2);

				Assert.AreEqual(354, totalHeight);
			}
		}

		[Test]
		public void Test_Query_Aggregate_Sum_With_Group()
		{
			var sum = (from student in this.model.Students
						group student by student.Id
									into g
									select g.Sum(x => x.Height)).FirstOrDefault();

			Assert.AreEqual(177, sum);
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
	}
}
