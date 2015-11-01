// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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
	public class DefaultIfEmptyTests
		: BaseTests<TestDataAccessModel>
	{
		private readonly DataAccessObjects<DefaultIfEmptyTestObject> queryable;
	
		public DefaultIfEmptyTests(string providerName)
			: base(providerName)
		{
			using (var scope = new TransactionScope())
			{
				var obj = this.model.DefaultIfEmptyTestObjects.Create();
				obj.Integer = 30;

				obj = this.model.DefaultIfEmptyTestObjects.Create();
				obj.Integer = 40;

				obj = this.model.DefaultIfEmptyTestObjects.Create();
				obj.NullableInteger = 30;

				obj = this.model.DefaultIfEmptyTestObjects.Create();
				obj.NullableInteger = 40;

				scope.Complete();
			}

			this.queryable = this.model.DefaultIfEmptyTestObjects;
		}

		[Test]
		public virtual void Test_Count_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var result = this.queryable.Count(c => c.Id < 0);
				var expectedResult = this.queryable.AsEnumerable().Count(c => c.Id < 0);

				Assert.AreEqual(expectedResult, result);
				Assert.AreEqual(0, result);
			}
		}

		[Test]
		public virtual void Test_Count_With_DefaultIfEmpty()
		{
			using (var scope = new TransactionScope())
			{
				var result = this.queryable.Where(c => c.Id < 0).DefaultIfEmpty().Count();
				var expectedResult = this.queryable.AsEnumerable().Where(c => c.Id < 0).DefaultIfEmpty().Count();

				Assert.AreEqual(expectedResult, result);
				Assert.AreEqual(1, result);
			}
		}


		[Test]
		public virtual void Test_Nullable_Select_Then_Count_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).Count();
				var expectedValue = this.queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).Count();

				Assert.AreEqual(value, expectedValue);
				Assert.AreEqual(0, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_Then_Count_Empty_Using_Nullable_Cast()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.Integer < 0).Select(c => (int?)c.Integer).Count();
				var expectedValue = this.queryable.ToList().Where(c => c.Integer < 0).Select(c => (int?)c.Integer).Count();

				Assert.AreEqual(value, expectedValue);
				Assert.AreEqual(0, value);
			}
		}

		/* Nullable & MAX */

		[Test]
		public virtual void Test_Nullable_Select_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.NullableInteger).Max();
				var expectedValue = this.queryable.ToList().Select(c => c.NullableInteger).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).Max();
				var expectedValue = this.queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.IsNull(value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.NullableInteger).DefaultIfEmpty().Max();
				var expectedValue = this.queryable.ToList().Select(c => c.NullableInteger).DefaultIfEmpty().Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty().Max();
				var expectedValue = this.queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty().Max();

				Assert.AreEqual(expectedValue, value);
				Assert.IsNull(null);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty707_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.NullableInteger).ToList().DefaultIfEmpty(707).Max();
				var expectedValue = this.queryable.ToList().Select(c => c.NullableInteger).ToList().DefaultIfEmpty(707).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty707_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty(707).Max();
				var expectedValue = this.queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty(707).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(707, value);
			}
		}

		/* Nullable & SUM */

		[Test]
		public virtual void Test_Nullable_Select_Then_Sum()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.NullableInteger).Sum();
				var expectedValue = this.queryable.ToList().Select(c => c.NullableInteger).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).Sum();
				var expectedValue = this.queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(0, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty_Then_Sum()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.NullableInteger).DefaultIfEmpty().Sum();
				var expectedValue = this.queryable.ToList().Select(c => c.NullableInteger).DefaultIfEmpty().Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty().Sum();
				var expectedValue = this.queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty().Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(0, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty707_Then_Sum()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.NullableInteger).DefaultIfEmpty(707).Sum();
				var expectedValue = this.queryable.ToList().Select(c => c.NullableInteger).DefaultIfEmpty(707).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty707_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty(707).Sum();
				var expectedValue = this.queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty(707).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(707, value);
			}
		}

		/* Integer & MAX */

		[Test]
		public virtual void Test_Integer_Select_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.Integer).Max();
				var expectedValue = this.queryable.ToList().Select(c => c.Integer).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				Assert.Throws<InvalidOperationException>(()=>
				{
					this.queryable.Where(c => c.Integer < 0).Select(c => c.Integer).Max();
				});

				Assert.Throws<InvalidOperationException>(() =>
				{
					this.queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).Max();
				});
			}
		}

		[Test]
		public virtual void Test_Integer_Select_Then_Max_Empty_Using_Nullable_Cast()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.Integer < 0).Select(c => (int?)c.Integer).Max();
				var expectedValue = this.queryable.ToList().Where(c => c.Integer < 0).Select(c => (int?)c.Integer).Max();

				Assert.AreEqual(value, expectedValue);
				Assert.AreEqual(null, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.Integer).DefaultIfEmpty().Max();
				var expectedValue = this.queryable.ToList().Select(c => c.Integer).DefaultIfEmpty().Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty().Max();
				var expectedValue = this.queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty().Max();

				Assert.AreEqual(expectedValue, value);
				Assert.IsNull(null);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty707_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.Integer).DefaultIfEmpty(707).Max();
				var expectedValue = this.queryable.ToList().Select(c => c.Integer).DefaultIfEmpty(707).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty707_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty(707).Max();
				var expectedValue = this.queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty(707).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(707, value);
			}
		}

		/* Integer & SUM */

		[Test]
		public virtual void Test_Integer_Select_Then_Sum()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.Integer).Sum();
				var expectedValue = this.queryable.ToList().Select(c => c.Integer).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.Integer < 0).Select(c => c.Integer).Sum();
				var expectedValue = this.queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(0, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty_Then_Sum()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.Integer).DefaultIfEmpty().Sum();
				var expectedValue = this.queryable.ToList().Select(c => c.Integer).DefaultIfEmpty().Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty().Sum();
				var expectedValue = this.queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty().Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(0, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty707_Then_Sum()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Select(c => c.Integer).DefaultIfEmpty(707).Sum();
				var expectedValue = this.queryable.ToList().Select(c => c.Integer).DefaultIfEmpty(707).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty707_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = this.queryable.Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty(707).Sum();
				var expectedValue = this.queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty(707).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(707, value);
			}
		}

		/* OBJ */

		[Test]
		public virtual void Test_Object_Select_DefaultIfEmpty()
		{
			using (var scope = new TransactionScope())
			{
				var obj = this.queryable.Create();
				var value = this.queryable.DefaultIfEmpty(obj).ToList();
				Assert.AreNotEqual(obj, value.First());

				var valueInMemory = this.queryable.ToList().DefaultIfEmpty(obj);
				Assert.AreEqual(value.First(), valueInMemory.First());
			}
		}

		[Test]
		public virtual void Test_Object_Select_DefaultIfEmpty_First()
		{
			using (var scope = new TransactionScope())
			{
				var obj = this.queryable.Create();
				var value = this.queryable.DefaultIfEmpty(obj).First();
				Assert.AreNotEqual(obj, value);

				var valueInMemory = this.queryable.ToList().DefaultIfEmpty(obj).First();
				Assert.AreEqual(value, valueInMemory);
			}
		}

		[Test]
		public virtual void Test_Object_Select_DefaultIfEmpty_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var obj = this.queryable.Create();
				var value = this.queryable.Where(c => c.Id < 0).DefaultIfEmpty(obj).ToList();

				Assert.AreEqual(obj, value.First());

				var valueInMemory = this.queryable.ToList().Where(c => c.Id < 0).DefaultIfEmpty(obj);
				Assert.AreEqual(value.First(), valueInMemory.First());
			}
		}

		[Test]
		public virtual void Test_Object_Select_DefaultIfEmpty_First_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var obj = this.queryable.Create();
				var value = this.queryable.Where(c => c.Id < 0).DefaultIfEmpty(obj).First();

				Assert.AreEqual(obj, value);

				var valueInMemory = this.queryable.ToList().Where(c => c.Id < 0).DefaultIfEmpty(obj).First();
				Assert.AreEqual(value, valueInMemory);
			}
		}
	}
}
