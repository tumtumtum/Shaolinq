// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql:DataAccessScope")]
	[TestFixture("Postgres:DataAccessScope")]
	[TestFixture("Postgres.DotConnect:DataAccessScope")]
	[TestFixture("Postgres.DotConnect.Unprepared:DataAccessScope")]
	[TestFixture("Sqlite")]
	[TestFixture("Sqlite:DataAccessScope")]
	[TestFixture("SqlServer:DataAccessScope")]
	[TestFixture("SqliteInMemory:DataAccessScope")]
	[TestFixture("SqliteClassicInMemory:DataAccessScope")]
	public class TestValueTypesWithDefaultValues
		: BaseTests<TestDataAccessModel>
	{
		public TestValueTypesWithDefaultValues(string providerName)
			: base(providerName, alwaysSubmitDefaultValues: false, valueTypesAutoImplicitDefault: false)
		{
		}
			
		[Test]
		public void Test_Student_MissingPropertyValue()
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.Create();
				var student = school.Students.Create();
				
				student.FavouriteDate = DateTime.Now;

				scope.Flush();
				scope.Complete();
			}
		}

		[Test]
		public void Test_Points_MissingPropertyValue()
		{
			Assert.Throws<MissingPropertyValueException>(() =>
			{
				using (var scope = NewTransactionScope())
				{
					var lecturer = this.model.Lecturers.Create();
					var paper = lecturer.Papers.Create();

					paper.Id = "COSC-230";
					Assert.That(paper.GetAdvanced().GetChangedProperties().Count, Is.EqualTo(2));

					scope.Flush();

					scope.Complete();
				}
			});
		}

		[Test]
		public void Test_Points_Not_MissingPropertyValue()
		{
			using (var scope = NewTransactionScope())
			{
				var lecturer = this.model.Lecturers.Create();
				var paper = lecturer.Papers.Create();

				paper.Id = "COSC-110";
				paper.Points = 100;

				Assert.That(paper.GetAdvanced().GetChangedProperties().Count, Is.EqualTo(3));

				paper.ExtraPoints2 = 10;

				Assert.That(paper.GetAdvanced().GetChangedProperties().Count, Is.EqualTo(4));

				scope.Flush();

				var paper2 = this.model.Papers.FirstOrDefault(c => c.Points == 100);

				Assert.AreEqual(paper.Id, paper2.Id);
				Assert.AreEqual(0, paper.ExtraPoints1);
				Assert.AreEqual(10, paper.ExtraPoints2);

				scope.Complete();
			}
		}
	}
}
