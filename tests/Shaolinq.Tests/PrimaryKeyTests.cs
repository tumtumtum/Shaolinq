using System;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class PrimaryKeyTests
		: BaseTests
	{
		public PrimaryKeyTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Create_Object_With_Guid_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithGuidAutoIncrementPrimaryKeys.NewDataAccessObject();

				// Should not be able to set AutoIncrement property

				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = Guid.NewGuid());

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_AutoIncrement_PrimaryKey_And_Get_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithGuidAutoIncrementPrimaryKeys.NewDataAccessObject();

				// AutoIncrement Guid  properties are set immediately

				Assert.IsTrue(obj.Id != Guid.Empty);
				
				scope.Flush(model);

				Assert.IsTrue(obj.Id != Guid.Empty);

				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = Guid.NewGuid());

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj1 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.NewDataAccessObject();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_Non_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.NewDataAccessObject();

				obj.Id = Guid.NewGuid();

				// Should not be able to set primary key twice

				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = Guid.NewGuid());

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey_Multiple()
		{
			Assert.Catch<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var obj1 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.NewDataAccessObject();
					var obj2 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.NewDataAccessObject();

					// Both objects will have same primary key (Guid.Empty)

					scope.Complete();
				}
			});
		}
		
		[Test]
		public void Test_Create_Object_With_Long_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongAutoIncrementPrimaryKeys.NewDataAccessObject();

				// Should not be able to set AutoIncrement properties

				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = 1);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Long_AutoIncrement_PrimaryKey_And_Get_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj1 = model.ObjectWithLongAutoIncrementPrimaryKeys.NewDataAccessObject();
				var obj2 = model.ObjectWithLongAutoIncrementPrimaryKeys.NewDataAccessObject();

				scope.Flush(model);

				Assert.AreEqual(1, obj1.Id);
				Assert.AreEqual(2, obj2.Id);

				// Should not be able to set AutoIncrement properties
				
				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj1.Id = 10);
				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj2.Id = 20);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Long_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey()
		{
			var name = new StackTrace().GetFrame(0).GetMethod().Name;

			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongNonAutoIncrementPrimaryKeys.NewDataAccessObject();

				obj.Name = name;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				Assert.AreEqual(1,  this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.Count(c => c.Name == name));
				Assert.AreEqual(0, this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.FirstOrDefault(c => c.Name == name).Id);
			}
		}

		[Test]
		public void Test_Create_Object_With_Long_Non_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			var name = new StackTrace().GetFrame(0).GetMethod().Name;

			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongNonAutoIncrementPrimaryKeys.NewDataAccessObject();

				obj.Id = 999;
				obj.Name = name;

				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = 1);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				Assert.AreEqual(1, this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.Count(c => c.Name == name));
				Assert.AreEqual(999, this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.FirstOrDefault(c => c.Name == name).Id);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Long_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey_Multiple()
		{
			Assert.Catch<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var obj1 = model.ObjectWithLongNonAutoIncrementPrimaryKeys.NewDataAccessObject();
					var obj2 = model.ObjectWithLongNonAutoIncrementPrimaryKeys.NewDataAccessObject();

					// Both objects will have same primary key (Guid.Empty)

					scope.Complete();
				}
			});
		}
	}
}
