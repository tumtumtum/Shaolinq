// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Sqlite")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	public class DeflatedReferenceTests
		: BaseTests
	{
		public DeflatedReferenceTests(string providerName)
			: base(providerName)
		{	
		}

		[Test]
		public void Test_Set_Related_Parent_Using_Deflated_Reference()
		{
			long schoolId;
			Guid studentId;
			const string schoolName = "American Kung Fu School";

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = schoolName;

				scope.Flush(model);

				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.Create();

				student.School = this.model.Schools.ReferenceTo(schoolId);

				scope.Flush(model);

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.First(c => c.Id == studentId);

				Assert.AreEqual(schoolId, student.School.Id);
				Assert.AreEqual(schoolName, student.School.Name);

				scope.Complete();
			}
		}

		[Test, ExpectedException(typeof(TransactionAbortedException))]
		public void Test_Set_Related_Parent_Using_Invalid_Deflated_Reference()
		{
			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.Create();

				student.School = this.model.Schools.ReferenceTo(8972394);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Use_Deflated_Reference_To_Update_Object_That_Was_Deleted1()
		{
			long schoolId;

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.Create();

				scope.Flush(model);

				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				model.Schools.DeleteWhere(c => c.Id == schoolId);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.ReferenceTo(schoolId);

				school.Name = "The Temple";

				Assert.Catch<MissingDataAccessObjectException>(() =>
				{
					scope.Flush(model);
				});
			}
		}

		[Test]
		public void Test_Use_Deflated_Reference_To_Update_Object_That_Was_Deleted2()
		{
			long schoolId;

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.Create();

				scope.Flush(model);

				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.ReferenceTo(schoolId);

				school.Delete();

				Assert.Catch<DeletedDataAccessObjectException>(() =>
				{
					school.Name = "The Temple";
				});

				scope.Flush(model);
				scope.Complete();
			}
		}

		[Test]
		public void Test_Use_Deflated_Reference_To_Update_Object_Without_First_Reading()
		{
			long schoolId;

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.Create();

				scope.Flush(model);

				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.ReferenceTo(schoolId);

				school.Name = "The Temple";

				scope.Complete();
			}
		}

		[Test, ExpectedException(typeof(TransactionAbortedException))]
		public void Test_Use_Deflated_Reference_To_Update_Non_Existant_Object_Without_First_Reading()
		{
			using (var scope = new TransactionScope())
			{
				var school = model.Schools.ReferenceTo(89327493);

				school.Name = "The Temple";

				Assert.Catch<InvalidDataAccessObjectAccessException>(() =>
				{
					scope.Flush(model);
				});

				scope.Complete();
			}
		}

		[Test]
		public void Test_Get_Deflated_Reference_From_Object_With_Non_AutoIncrementing_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongNonAutoIncrementPrimaryKeys.Create();

				obj.Id = 1077;

				var obj2 =  model.GetReferenceByPrimaryKey<ObjectWithLongNonAutoIncrementPrimaryKey>(1077);

				Assert.AreEqual(obj.Id, obj2.Id);
				Assert.AreSame(obj, obj2);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Student_Then_Access_Best_Friend()
		{
			using (var scope = new TransactionScope())
			{
				var school = model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

				student.Birthdate = new DateTime(1940, 11, 27);
				student.Firstname = "Bruce";
				student.Lastname = "Lee";

				var friend = school.Students.Create();
				
				friend.Firstname = "Chuck";
				friend.Lastname = "Norris";

				if (this.ProviderName == "MySql")
				{
					scope.Flush(model);
				}

				student.BestFriend = friend;

				scope.Flush(model);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.First(c => c.Firstname == "Bruce");

				Assert.IsTrue(student.BestFriend.IsDeflatedReference);
				Assert.AreEqual("Chuck", student.BestFriend.Firstname);
				Assert.IsFalse(student.BestFriend.IsDeflatedReference);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Student_Then_Access_School_As_DeflatedReference()
		{
			Guid studentId;
			long schoolId;

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

				student.Birthdate = new DateTime(1940, 11, 27);
				student.Firstname = "Bruce";
				student.Lastname = "Lee";

				scope.Flush(model);

				studentId = student.Id;
				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.GetReferenceByPrimaryKey<Student>(new { Id = studentId });

				Assert.IsTrue(student.IsDeflatedReference);

				Assert.AreEqual("Bruce", student.Firstname);

				Assert.IsFalse(student.IsDeflatedReference);

				Assert.AreEqual("Bruce", student.Firstname);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.GetReferenceByPrimaryKey<Student>(studentId);

				Assert.IsTrue(student.IsDeflatedReference);

				Assert.AreEqual("Bruce", student.Firstname);

				Assert.IsFalse(student.IsDeflatedReference);

				Assert.AreEqual("Bruce", student.Firstname);

				scope.Complete();
			}


			using (var scope = new TransactionScope())
			{
				var student = this.model.GetReferenceByPrimaryKey<Student>(studentId);

				Assert.IsTrue(student.IsDeflatedReference);

				var sameStudent = this.model.Students.First(c => c.Id == studentId);

				Assert.IsFalse(student.IsDeflatedReference);

				Assert.AreSame(student, sameStudent);
				Assert.AreEqual("Bruce", student.Firstname);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.GetReferenceByPrimaryKey<Student>(studentId);

				Assert.IsTrue(student.IsDeflatedReference);

				student.Inflate();

				Assert.IsFalse(student.IsDeflatedReference);

				Assert.AreEqual("Bruce", student.Firstname);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = model.Students.First(c => c.Id == studentId);

				Assert.AreEqual(schoolId, student.School.Id);
				Assert.IsTrue(student.School.IsDeflatedReference);

				Assert.AreEqual("The Shaolinq School of Kung Fu", student.School.Name);

				Assert.IsFalse(student.School.IsDeflatedReference);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = model.Students.First(c => c.Id == studentId);

				Assert.AreEqual(schoolId, student.School.Id);
				Assert.IsTrue(student.School.IsDeflatedReference);

				var school = model.Schools.FirstOrDefault(c => c.Id == schoolId);

				Assert.IsFalse(student.School.IsDeflatedReference);

				Assert.AreEqual(school, student.School);

				scope.Complete();
			}
		}
	}
}
