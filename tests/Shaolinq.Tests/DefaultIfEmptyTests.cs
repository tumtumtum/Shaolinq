using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.DataModels.Test;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class DefaultIfEmptyTests
		: BaseTests
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

			queryable = this.model.DefaultIfEmptyTestObjects;
		}

		[Test]
		public virtual void Test_Count()
		{
			using (var scope = new TransactionScope())
			{
				var result = queryable.Where(c => c.Id < 0).Count();
				var expectedResult = queryable.ToList().Where(c => c.Id < 0).Count();

				Assert.AreEqual(expectedResult, result);
				Assert.AreEqual(0, result);
			}
		}

		[Test]
		public virtual void Test_Count_With_DefaultIfEmpty()
		{
			using (var scope = new TransactionScope())
			{
				var result = queryable.Where(c => c.Id < 0).DefaultIfEmpty().Count();
				var expectedResult = queryable.ToList().Where(c => c.Id < 0).DefaultIfEmpty().Count();

				Assert.AreEqual(expectedResult, result);
				Assert.AreEqual(1, result);
			}
		}

		/* Nullable & MAX */

		[Test]
		public virtual void Test_Nullable_Select_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Select(c => c.NullableInteger).Max();
				var expectedValue = queryable.ToList().Select(c => c.NullableInteger).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).Max();
				var expectedValue = queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.IsNull(value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Select(c => c.NullableInteger).DefaultIfEmpty().Max();
				var expectedValue = queryable.ToList().Select(c => c.NullableInteger).DefaultIfEmpty().Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty().Max();
				var expectedValue = queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty().Max();

				Assert.AreEqual(expectedValue, value);
				Assert.IsNull(null);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty707_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Select(c => c.NullableInteger).DefaultIfEmpty(707).Max();
				var expectedValue = queryable.ToList().Select(c => c.NullableInteger).DefaultIfEmpty(707).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty707_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty(707).Max();
				var expectedValue = queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty(707).Max();

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
				var value = queryable.Select(c => c.NullableInteger).Sum();
				var expectedValue = queryable.ToList().Select(c => c.NullableInteger).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).Sum();
				var expectedValue = queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(0, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty_Then_Sum()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Select(c => c.NullableInteger).DefaultIfEmpty().Sum();
				var expectedValue = queryable.ToList().Select(c => c.NullableInteger).DefaultIfEmpty().Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty().Sum();
				var expectedValue = queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty().Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(0, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty707_Then_Sum()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Select(c => c.NullableInteger).DefaultIfEmpty(707).Sum();
				var expectedValue = queryable.ToList().Select(c => c.NullableInteger).DefaultIfEmpty(707).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Nullable_Select_DefaultIfEmpty707_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty(707).Sum();
				var expectedValue = queryable.ToList().Where(c => c.NullableInteger < 0).Select(c => c.NullableInteger).DefaultIfEmpty(707).Sum();

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
				var value = queryable.Select(c => c.Integer).Max();
				var expectedValue = queryable.ToList().Select(c => c.Integer).Max();

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
					var value = queryable.Where(c => c.Integer < 0).Select(c => c.Integer).Max();
				});

				Assert.Throws<InvalidOperationException>(() =>
				{
					var expectedValue = queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).Max();
				});
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Select(c => c.Integer).DefaultIfEmpty().Max();
				var expectedValue = queryable.ToList().Select(c => c.Integer).DefaultIfEmpty().Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty().Max();
				var expectedValue = queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty().Max();

				Assert.AreEqual(expectedValue, value);
				Assert.IsNull(null);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty707_Then_Max()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Select(c => c.Integer).DefaultIfEmpty(707).Max();
				var expectedValue = queryable.ToList().Select(c => c.Integer).DefaultIfEmpty(707).Max();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(40, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty707_Then_Max_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty(707).Max();
				var expectedValue = queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty(707).Max();

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
				var value = queryable.Select(c => c.Integer).Sum();
				var expectedValue = queryable.ToList().Select(c => c.Integer).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.Integer < 0).Select(c => c.Integer).Sum();
				var expectedValue = queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(0, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty_Then_Sum()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Select(c => c.Integer).DefaultIfEmpty().Sum();
				var expectedValue = queryable.ToList().Select(c => c.Integer).DefaultIfEmpty().Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty().Sum();
				var expectedValue = queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty().Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(0, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty707_Then_Sum()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Select(c => c.Integer).DefaultIfEmpty(707).Sum();
				var expectedValue = queryable.ToList().Select(c => c.Integer).DefaultIfEmpty(707).Sum();

				Assert.AreEqual(expectedValue, value);
				Assert.AreEqual(70, value);
			}
		}

		[Test]
		public virtual void Test_Integer_Select_DefaultIfEmpty707_Then_Sum_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var value = queryable.Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty(707).Sum();
				var expectedValue = queryable.ToList().Where(c => c.Integer < 0).Select(c => c.Integer).DefaultIfEmpty(707).Sum();

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
				var obj = queryable.Create();
				var value = queryable.DefaultIfEmpty(obj).ToList();
				Assert.AreNotEqual(obj, value.First());

				var valueInMemory = queryable.ToList().DefaultIfEmpty(obj);
				Assert.AreEqual(value.First(), valueInMemory.First());
			}
		}

		[Test]
		public virtual void Test_Object_Select_DefaultIfEmpty_First()
		{
			using (var scope = new TransactionScope())
			{
				var obj = queryable.Create();
				var value = queryable.DefaultIfEmpty(obj).First();
				Assert.AreNotEqual(obj, value);

				var valueInMemory = queryable.ToList().DefaultIfEmpty(obj).First();
				Assert.AreEqual(value, valueInMemory);
			}
		}

		[Test]
		public virtual void Test_Object_Select_DefaultIfEmpty_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var obj = queryable.Create();
				var value = queryable.Where(c => c.Id < 0).DefaultIfEmpty(obj).ToList();

				Assert.AreEqual(obj, value.First());

				var valueInMemory = queryable.ToList().Where(c => c.Id < 0).DefaultIfEmpty(obj);
				Assert.AreEqual(value.First(), valueInMemory.First());
			}
		}

		[Test]
		public virtual void Test_Object_Select_DefaultIfEmpty_First_Empty()
		{
			using (var scope = new TransactionScope())
			{
				var obj = queryable.Create();
				var value = queryable.Where(c => c.Id < 0).DefaultIfEmpty(obj).First();

				Assert.AreEqual(obj, value);

				var valueInMemory = queryable.ToList().Where(c => c.Id < 0).DefaultIfEmpty(obj).First();
				Assert.AreEqual(value, valueInMemory);
			}
		}
	}
}
