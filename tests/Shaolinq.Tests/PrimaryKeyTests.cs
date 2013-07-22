using System;
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

				// Should be able to set a GUID value
				obj.Id = Guid.NewGuid();

				// Should not be able to set GUID value twice
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

				scope.Flush(model);

				Assert.IsTrue(obj.Id != Guid.Empty);

				// Should not be able to set GUID once its accessed
				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = Guid.NewGuid());

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Long_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongAutoIncrementPrimaryKeys.NewDataAccessObject();

				// Should be able to set the long
				obj.Id = 1;

				// Should not be able to set long value twice
				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = 2);

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

				// Should not be able to set GUID once its accessed
				
				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj1.Id = 10);
				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj2.Id = 20);

				scope.Complete();
			}
		}
	}
}
