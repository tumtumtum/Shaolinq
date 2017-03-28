using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
	[TestFixture("SqlServer:DataAccessScope", Category = "IgnoreOnMono")]
	[TestFixture("SqliteInMemory:DataAccessScope")]
	[TestFixture("SqliteClassicInMemory:DataAccessScope")]
	public class TestValueTypesWithDefaultValues
		: BaseTests<TestDataAccessModel>
	{
		public TestValueTypesWithDefaultValues(string providerName)
			: base(providerName)
		{
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

				bool predicated;
				Console.WriteLine(string.Join(",", paper.ToObjectInternal().GetChangedPropertiesFlattened(out predicated).Select(c => c.PropertyName)));
				Assert.That(paper.GetAdvanced().GetChangedProperties().Count, Is.EqualTo(3));
				
				scope.Flush();
				
				var paper2 = this.model.Papers.FirstOrDefault(c => c.Points == 100);

				Assert.AreEqual(paper.Id, paper2.Id);
				Assert.AreEqual(0, paper.ExttraPoints1);
				Assert.AreEqual(10, paper.ExttraPoints2);

				scope.Complete();
			}
		}
	}
}
