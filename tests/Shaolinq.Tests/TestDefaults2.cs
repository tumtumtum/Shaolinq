using System.Linq;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite")]
	[TestFixture("Sqlite:DataAccessScope")]
	[TestFixture("SqlServer", Category = "IgnoreOnMono")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	public class TestDefaults2
		: BaseTests<TestDataAccessModel>
	{
		public TestDefaults2(string providerName)
			: base(providerName, alwaysSubmitDefaultValues: false, valueTypesAutoImplicitDefault: false, includeImplicitDefaultsInSchema: true)
		{
		}

		[Test]
		public void Test_Not_Set_Values1()
		{
			Assert.Throws<MissingPropertyValueException>(() =>
			{
				using (var scope = NewTransactionScope())
				{
					var obj = this.model.DefaultsTestObjects.Create();

					obj.IntValueWithValueRequired = 1;
					obj.NullableIntValueWithValueRequired = 2;

					scope.Flush();
					scope.Complete();
				}
			});
		}

		[Test]
		public void Test_Not_Set_Values2()
		{
			Assert.Throws<MissingPropertyValueException>(() =>
			{
				using (var scope = NewTransactionScope())
				{
					var obj = this.model.DefaultsTestObjects.Create();

					obj.IntValue = 0;
					obj.NullableIntValueWithValueRequired = 2;

					scope.Flush();
					scope.Complete();
				}
			});
		}

		[Test]
		public void Test_Not_All_Values_Set1()
		{
			long id;

			using (var scope = NewTransactionScope())
			{
				var obj = this.model.DefaultsTestObjects.Create();

				obj.IntValue = 0;
				obj.NullableIntValue = null;
				obj.IntValueWithValueRequired = 1;
				obj.NullableIntValueWithValueRequired = 2;

				scope.Flush();

				id = obj.Id;

				scope.Complete();
			}

			this.model.DefaultsTestObjects.Single(c => c.Id == id);
		}

		[Test]
		public void Test_Not_All_Values_Set2()
		{
			long id;

			using (var scope = NewTransactionScope())
			{
				var obj = this.model.DefaultsTestObjects.Create();

				obj.IntValue = 0;
				obj.IntValueWithValueRequired = 1;
				obj.NullableIntValueWithValueRequired = 2;

				scope.Flush();

				id = obj.Id;

				scope.Complete();
			}

			this.model.DefaultsTestObjects.Single(c => c.Id == id);
		}
	}
}
