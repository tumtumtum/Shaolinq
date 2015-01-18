// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Persistence;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("SqlServer")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite", Category = "SupportsSqlite")]
	[TestFixture("SqliteInMemory", Category = "SupportsSqlite")]
	[TestFixture("SqliteClassicInMemory", Category = "SupportsSqlite")]
	public class PrimaryKeyTests
		: BaseTests<TestDataAccessModel>
	{
		public PrimaryKeyTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_AutoIncrement_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj1 = model.Schools.Create();
				scope.Flush(model);

				var obj2 = model.Schools.Create();
				scope.Flush(model);
				Assert.Greater(obj2.Id, obj1.Id);

				var obj3 = model.Schools.Create();
				scope.Flush(model);
				Assert.Greater(obj3.Id, obj2.Id);

				var obj4 = model.Schools.Create();
				scope.Flush(model); 
				Assert.Greater(obj4.Id, obj3.Id);
				
				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithGuidAutoIncrementPrimaryKeys.Create();

				obj.Id = Guid.NewGuid();
					
				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_AutoIncrement_PrimaryKey_And_Get_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithGuidAutoIncrementPrimaryKeys.Create();

				// AutoIncrement Guid  properties are set immediately

				Assert.IsTrue(obj.Id != Guid.Empty);
				
				scope.Flush(model);

				Assert.IsTrue(obj.Id != Guid.Empty);

				obj.Id = Guid.NewGuid();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj1 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.Create();

				obj1.Id = Guid.NewGuid();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_Non_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.Create();

				obj.Id = Guid.NewGuid();

				// Should be able to set primary key twice

				obj.Id = Guid.NewGuid();

				scope.Complete();
			}
		}

		[Test, ExpectedException(typeof(ObjectAlreadyExistsException))]
		public void Test_Create_Objects_With_Guid_Non_AutoIncrement_PrimaryKey_And_Set_Same_Primary_Keys_With_Assign()
		{
			try
			{
				using (var scope = new TransactionScope())
				{
					var id = Guid.NewGuid();

					var obj1 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.Create(id);
					var obj2 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.Create();

					obj2.Id = id;

					scope.Complete();
				}
			}
			catch (TransactionAbortedException e)
			{
				throw e.InnerException;
			}
		}

		[Test, ExpectedException(typeof(ObjectAlreadyExistsException))]
		public void Test_Create_Objects_With_Guid_Non_AutoIncrement_PrimaryKey_And_Set_Same_Primary_Keys_With_Create()
		{
			try
			{
				using (var scope = new TransactionScope())
				{
					var id = Guid.NewGuid();

					var obj1 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.Create(id);
					var obj2 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.Create(id);
					
					scope.Complete();
				}
			}
			catch (TransactionAbortedException e)
			{
				throw e.InnerException;
			}
		}
		
		[Test]
		public void Test_Create_Object_With_Long_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongAutoIncrementPrimaryKeys.Create();

				obj.Id = 10007;

				Assert.IsTrue(obj.GetAdvanced().GetChangedProperties().Any(c => c.PropertyName == "Id"));
				Assert.IsTrue(obj.GetAdvanced().GetChangedPropertiesFlattened().Any(c => c.PropertyName == "Id"));

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongAutoIncrementPrimaryKeys.GetByPrimaryKey(10007);
			}
		}

		[Test]
		public void Test_Create_Object_With_Long_AutoIncrement_PrimaryKey_And_Get_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj1 = model.ObjectWithLongAutoIncrementPrimaryKeys.Create();
				var obj2 = model.ObjectWithLongAutoIncrementPrimaryKeys.Create();

				scope.Flush(model);

				Assert.AreEqual(1, obj1.Id);
				Assert.AreEqual(2, obj2.Id);

				if (this.model.GetCurrentSqlDatabaseContext().SqlDialect.SupportsFeature(SqlFeature.UpdateAutoIncrementColumns))
				{
					obj1.Id = 100;
					obj2.Id = 200;
				}

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Long_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey()
		{
			var name = new StackTrace().GetFrame(0).GetMethod().Name;

			Assert.Throws<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var obj = model.ObjectWithLongNonAutoIncrementPrimaryKeys.Create();

					obj.Name = name;

					scope.Complete();
				}
			});
		}

		[Test]
		public void Test_Create_Object_With_Long_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey_With_Default_Value()
		{
			var name = new StackTrace().GetFrame(0).GetMethod().Name;

			Assert.Throws<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var obj = model.ObjectWithLongNonAutoIncrementPrimaryKeys.Create();

					obj.Id = 0;
					obj.Name = name;

					scope.Complete();
				}
			});
		}


		[Test]
		public void Test_Create_Object_With_Long_Non_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			var name = new StackTrace().GetFrame(0).GetMethod().Name;

			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongNonAutoIncrementPrimaryKeys.Create();

				obj.Id = 999;
				obj.Name = name;

				obj.Id = 1;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				Assert.AreEqual(1, this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.Count(c => c.Name == name));
				Assert.AreEqual(1, this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.First(c => c.Name == name).Id);
				Assert.AreEqual(1, this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.FirstOrDefault(c => c.Name == name).Id);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Composite_Primary_Keys()
		{
			var secondaryKey = MethodBase.GetCurrentMethod().Name;
			Guid studentId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();
				var student = school.Students.Create();

				scope.Flush(model);

				var obj1 = this.model.ObjectWithCompositePrimaryKeys.Create();
				
				// Can access non auto increment
				Console.WriteLine(obj1.Id);

				obj1.Id = 1;
				obj1.SecondaryKey = secondaryKey;
				obj1.Name = "Obj1";
				obj1.Student = student;

				var obj2 = this.model.ObjectWithCompositePrimaryKeys.Create();
				obj2.Id = 2;
				obj2.SecondaryKey = secondaryKey;
				obj2.Name = "Obj2";
				obj2.Student = student;

				scope.Flush(model);

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var obj1 = this.model.ObjectWithCompositePrimaryKeys.Single(c => c.SecondaryKey == secondaryKey && c.Id == 1);

				Assert.AreEqual(1, obj1.Id);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = model.Students.GetReference(studentId);

				var obj = this.model.ObjectWithCompositePrimaryKeys.GetReference(new
				{
					Id = 1,
					SecondaryKey = secondaryKey,
					Student = student
				});

				Assert.IsTrue(obj.IsDeflatedReference());

				Assert.AreEqual("Obj1", obj.Name);

				Assert.IsFalse(obj.IsDeflatedReference());

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				var obj1 = this.model.ObjectWithCompositePrimaryKeys.GetReference(new
				{
					Id = 1,
					SecondaryKey = secondaryKey,
					Student = student
				});

				Assert.IsTrue(obj1.IsDeflatedReference());

				var objs = this.model.ObjectWithCompositePrimaryKeys.Where(c => c == obj1 && c.Name != "");

				Assert.AreEqual(1, objs.Count());
				Assert.AreEqual(1, objs.Single().Id);

				objs = this.model.ObjectWithCompositePrimaryKeys.Where(c => c.SecondaryKey == secondaryKey);

				Assert.That(objs.Count(), Is.GreaterThan(1));

				obj1.Name = "new name";

				scope.Complete();
			}
		}

		[Test]
		public void Test_GetByPrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				scope.Flush(model);

				var school2 = this.model.Schools.GetByPrimaryKey(school.Id);
			}
		}


		[Test]
		public void Test_GetByPrimaryKey_Composite()
		{
			Guid studentId;
			var secondaryKey = MethodBase.GetCurrentMethod().Name;
			
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();
				var student = school.Students.Create();

				scope.Flush(model);

				studentId = student.Id;

				var obj1 = this.model.ObjectWithCompositePrimaryKeys.Create();

				obj1.Id = 88;
				obj1.SecondaryKey = secondaryKey;
				obj1.Name = "Name" + secondaryKey;
				obj1.Student = student;
				
				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = this.model.ObjectWithCompositePrimaryKeys.GetByPrimaryKey
				(
					new
					{
						Id = 88,
						SecondaryKey = secondaryKey,
						Student = model.Students.GetReference(studentId)
					}
				);
			}
		}
	}
}
