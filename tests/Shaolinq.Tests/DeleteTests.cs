// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	public class DeleteTests
		: BaseTests
	{
		public DeleteTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Use_Deflated_Reference_To_Update_Related_Object_That_Was_Deleted()
		{
			Guid student1Id, student2Id;

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.Create();
				
				scope.Flush(model);

				if (this.ProviderName == "MySql")
				{
					// MySql does not support deferred foriegn key checks so create student2 first

					var student2 = school.Students.Create();

					scope.Flush(model);

					var student1 = school.Students.Create();

					student1Id = student1.Id;
					student2Id = student2.Id;

					student1.BestFriend = student2;
				}
				else
				{
					var student1 = school.Students.Create(); 
					var student2 = school.Students.Create();

					student1Id = student1.Id;
					student2Id = student2.Id;

					student1.BestFriend = student2;
				}
				
				
				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				this.model.Students.DeleteWhere(c => c.Id == student2Id);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				Assert.IsNull(this.model.Students.FirstOrDefault(c => c.Id == student2Id));

				var student1 = model.Students.First(c => c.Id == student1Id);
				
				Assert.IsNull(student1.BestFriend);

				scope.Complete();
			}
		}
	
		[Test]
		public void Test_Object_Deleted_Flushed_Still_Deleted()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				Assert.IsFalse(school.IsDeleted);
				school.Delete();
				Assert.IsTrue(school.IsDeleted);
				scope.Flush(model);
				Assert.IsTrue(school.IsDeleted);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Modify_Deleted_Object()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Delete();

				Assert.Catch<DeletedDataAccessObjectException>(() =>
				{
					school.Name = "Hello";
				});

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_Then_Delete_Object_Then_Query_Then_Access()
		{
			long schoolId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "Yoga Decorum";

				scope.Flush(model);

				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.First(c => c.Id == schoolId);

				Assert.IsFalse(school.IsDeleted); 
				school.Delete();
				Assert.IsTrue(school.IsDeleted);

				school = this.model.Schools.First(c => c.Id == schoolId);

				Assert.IsTrue(school.IsDeleted);
			}

			using (var scope = new TransactionScope())
			{
				Assert.IsNotNull(this.model.Schools.Single(c => c.Id == schoolId));
			}

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.First(c => c.Id == schoolId);

				Assert.IsFalse(school.IsDeleted);
				school.Delete();
				Assert.IsTrue(school.IsDeleted);

				school = this.model.Schools.First(c => c.Id == schoolId);

				Assert.IsTrue(school.IsDeleted);
				Assert.AreEqual("Yoga Decorum", school.Name);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				Assert.IsNull(this.model.Schools.FirstOrDefault(c => c.Id == schoolId));
			}
		}

		[Test]
		public void Test_Query_Access_Deleted_Object_Via_DeflatedReference()
		{
			long schoolId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "Yoga Decorum";

				scope.Flush(model);

				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				this.model.Schools.DeleteWhere(c => c.Id == schoolId);

				scope.Complete();
			}

			Assert.Catch<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var school = this.model.Schools.ReferenceTo(schoolId);

					school.Name = "Yoga Decorum!!!";

					Assert.Catch<MissingDataAccessObjectException>(() =>
					{
						scope.Flush(model);
					});

					scope.Complete();
				}
			});
		}
	}
}
