// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Threading.Tasks;
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
	public class DeflatedReferenceTests
		: BaseTests<TestDataAccessModel>
	{
		public DeflatedReferenceTests(string providerName)
			: base(providerName)
		{	
		}

		[Test]
		[ExpectedException(typeof(MissingDataAccessObjectException))]
		public void Test_Inflate_Nonexistent_Object()
		{
			using (var scope = new TransactionScope())
			{
				var foo = this.model.Schools.GetReference(999);

				foo.Inflate();
			}
		}

		[Test]
		public void Test_Preloading_Reference()
		{
			long schoolId;
			const string schoolName = "Oxford";

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = schoolName;
				var student = school.Students.Create();
				student.Firstname = "Laurie";
				scope.Flush();
				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var schoolAndStudent = 
					(from school in this.model.Schools
					join student in this.model.Students
 					on school equals student.School
					select new {school, student}).ToList();

				var firstStudent = schoolAndStudent.FirstOrDefault().student;

				Assert.IsFalse(firstStudent.School.IsDeflatedReference());

				scope.Complete();
			}
		}

		[Test]
		public void Test_Get_Deflated_Reference_With_Boxed_Guid()
		{
			const string schoolName = "American Kung Fu School";
			Guid studentId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = schoolName;
				var student = school.Students.Create();

				scope.Flush();

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference((object)studentId);
			}
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

				scope.Flush();

				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.Create();

				student.School = this.model.Schools.GetReference(schoolId);

				scope.Flush();

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

		[Test, ExpectedException(typeof(MissingRelatedDataAccessObjectException))]
		public void Test_Set_Related_Parent_Using_Invalid_Deflated_Reference()
		{
			try
			{
				using (var scope = new TransactionScope())
				{
					var student = this.model.Students.Create();

					student.School = this.model.Schools.GetReference(8972394);
					scope.Complete();
				}
			}
			catch (TransactionAbortedException e)
			{
				throw e.InnerException;
			}
		}

		[Test, ExpectedException(typeof(MissingDataAccessObjectException))]
		public void Test_Use_Deflated_Reference_To_Update_Object_That_Was_Deleted1()
		{
			long schoolId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				scope.Flush();

				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
			    this.model.Schools.Where(c => c.Id == schoolId).Delete();

				scope.Complete();
			}

			try
			{
				using (var scope = new TransactionScope())
				{
					var school = this.model.Schools.GetReference(schoolId);

					school.Name = "The Temple";

					scope.Complete();
				}
			}
			catch (TransactionAbortedException e)
			{
				throw e.InnerException;
			}
		}

		[Test]
		public void Test_Use_Deflated_Reference_To_Update_Object_That_Was_Deleted2()
		{
			long schoolId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				scope.Flush();

				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.GetReference(schoolId);

				school.Delete();

				Assert.Catch<DeletedDataAccessObjectException>(() =>
				{
					school.Name = "The Temple";
				});

				scope.Complete();
			}
		}

		[Test]
		public void Test_Use_Deflated_Reference_To_Update_Object_Without_First_Reading()
		{
			long schoolId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				scope.Flush();

				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.GetReference(schoolId);

				school.Name = "The Temple";

				scope.Complete();
			}
		}

		[Test, ExpectedException(typeof(MissingDataAccessObjectException))]
		public void Test_Use_Deflated_Reference_To_Update_Non_Existant_Object_Without_First_Reading()
		{
			try
			{
				using (var scope = new TransactionScope())
				{
					var school = this.model.Schools.GetReference(89327493);

					school.Name = "The Temple";

					scope.Complete();
				}
			}
			catch (TransactionAbortedException e)
			{
				throw e.InnerException;
			}
		}

		[Test]
		public void Test_Get_Deflated_Reference_From_Object_With_Non_AutoIncrementing_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.Create();

				obj.Id = 1077;

				var obj2 = this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.GetReference(1077);

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
				var school = this.model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

				student.Birthdate = new DateTime(1940, 11, 27);
				student.Firstname = "Bruce";
				student.Lastname = "Lee";

				var friend = school.Students.Create();
				
				friend.Firstname = "Chuck";
				friend.Lastname = "Norris";

				student.BestFriend = friend;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.First(c => c.Firstname == "Bruce");

				Assert.IsTrue(student.BestFriend.IsDeflatedReference());
				Assert.AreEqual("Chuck", student.BestFriend.Firstname);
				Assert.IsFalse(student.BestFriend.IsDeflatedReference());

				scope.Complete();
			}
		}

		[Test]
		public void Test_Equals_With_Deflated()
		{
			Guid studentId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

				student.Birthdate = new DateTime(1940, 11, 27);

				scope.Flush();

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				Assert.IsTrue(student.IsDeflatedReference());

				var sameStudent = this.model.Students.First(c => c.Equals(student));

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreSame(student, sameStudent);

				scope.Complete();
			}
		}

		[Test]
		public void Test_IdEquals_With_Deflated()
		{
			Guid studentId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

				student.Birthdate = new DateTime(1940, 11, 27);

				scope.Flush();

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				Assert.IsTrue(student.IsDeflatedReference());

				var sameStudent = this.model.Students.SingleOrDefault(c => c.Id.Equals(student.Id));

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreSame(student, sameStudent);

				scope.Complete();
			}
		}

		[Test]
		public void Test_EqualsId_With_Deflated()
		{
			Guid studentId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

				student.Birthdate = new DateTime(1940, 11, 27);

				scope.Flush();

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				Assert.IsTrue(student.IsDeflatedReference());

				var sameStudent = this.model.Students.First(c => c.Id.Equals(studentId));

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreSame(student, sameStudent);

				scope.Complete();
			}
		}

		[Test]
		public void Test_ReferenceEquals_With_Deflated()
		{
			Guid studentId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

				student.Birthdate = new DateTime(1940, 11, 27);
				
				scope.Flush();

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				Assert.IsTrue(student.IsDeflatedReference());

				var sameStudent = this.model.Students.First(c => ReferenceEquals(c, student));

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreSame(student, sameStudent);

				scope.Complete();
			}
		}

		[Test]
		public void Test_CompareTo_With_Deflated()
		{
			Guid studentId;

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

				student.Birthdate = new DateTime(1940, 11, 27);
				
				scope.Flush();

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				Assert.IsTrue(student.IsDeflatedReference());

				var sameStudent = this.model.Students.First(c => c.CompareTo(student) == 0);

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreSame(student, sameStudent);

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
				var school = this.model.Schools.Create();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.Create();

				student.Birthdate = new DateTime(1940, 11, 27);
				student.Firstname = "Bruce";
				student.Lastname = "Lee";

				scope.Flush();

				studentId = student.Id;
				schoolId = school.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(new { Id = studentId });

				Assert.IsTrue(student.IsDeflatedReference());

				Assert.AreEqual("Bruce", student.Firstname);

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreEqual("Bruce", student.Firstname);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				Assert.IsTrue(student.IsDeflatedReference());

				Assert.AreEqual("Bruce", student.Firstname);

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreEqual("Bruce", student.Firstname);

				scope.Complete();
			}


			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				Assert.IsTrue(student.IsDeflatedReference());

				var sameStudent = this.model.Students.First(c => c.Id == studentId);

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreSame(student, sameStudent);
				Assert.AreEqual("Bruce", student.Firstname);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				Assert.IsTrue(student.IsDeflatedReference());

				var sameStudent = this.model.Students.First(c => c == student);

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreSame(student, sameStudent);
				Assert.AreEqual("Bruce", student.Firstname);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				Assert.IsTrue(student.IsDeflatedReference());

				student.Inflate();

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreEqual("Bruce", student.Firstname);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.GetReference(studentId);

				Assert.IsTrue(student.IsDeflatedReference());

				student.Inflate();

				Assert.IsFalse(student.IsDeflatedReference());

				Assert.AreEqual("Bruce", student.Firstname);

				scope.Complete();
			}

			Func<Task> func = async delegate
			{
				using (var scope = new DataAccessScope())
				{
					var student = this.model.Students.GetReference(studentId);

					Assert.IsTrue(student.IsDeflatedReference());

					await student.InflateAsync();

					Assert.IsFalse(student.IsDeflatedReference());
				}
			};

			func().ConfigureAwait(false).GetAwaiter().GetResult();

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.First(c => c.Id == studentId);

				Assert.AreEqual(schoolId, student.School.Id);
				Assert.IsTrue(student.School.IsDeflatedReference());

				Assert.AreEqual("The Shaolinq School of Kung Fu", student.School.Name);

				Assert.IsFalse(student.School.IsDeflatedReference());

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.First(c => c.Id == studentId);

				Assert.AreEqual(schoolId, student.School.Id);
				Assert.IsTrue(student.School.IsDeflatedReference());

				var school = this.model.Schools.FirstOrDefault(c => c.Id == schoolId);

				Assert.IsFalse(student.School.IsDeflatedReference());

				Assert.AreEqual(school, student.School);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Update_NonDeflated_Object_Field_To_Null()
		{
			long id;

			using (var scope = new TransactionScope())
			{
				var dbObj = this.model.ObjectWithManyTypes.Create();

				dbObj.String = "foo";
				dbObj.NullableDateTime = DateTime.UtcNow;

				scope.Flush();

				id = dbObj.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var dbObj = this.model.ObjectWithManyTypes.GetByPrimaryKey(id);

				dbObj.String = null;
				dbObj.NullableDateTime = null;

				Assert.That(((IDataAccessObjectAdvanced)dbObj).GetChangedProperties().Count, Is.EqualTo(2));

				scope.Complete();
			}
		}

		[Test]
		public void Test_Update_Deflated_Object_Field_To_Null()
		{
			long id;

			using (var scope = new TransactionScope())
			{
				var dbObj = this.model.ObjectWithManyTypes.Create();

				dbObj.String = "foo";
				dbObj.NullableDateTime = DateTime.UtcNow;

				scope.Flush();

				id = dbObj.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var dbObj = this.model.ObjectWithManyTypes.GetReference(id);

				dbObj.String = null;
				dbObj.NullableDateTime = null;

				Assert.That(((IDataAccessObjectAdvanced)dbObj).GetChangedProperties().Count, Is.EqualTo(2));

				scope.Complete();
			}
		}

		[Test]
		public void Test_Update_Deflated_Object_Field_To_Non_Null_Different_Value()
		{
			long id;

			using (var scope = new TransactionScope())
			{
				var dbObj = this.model.ObjectWithManyTypes.Create();

				dbObj.String = "foo";
				dbObj.NullableDateTime = DateTime.UtcNow;

				scope.Flush();

				id = dbObj.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var dbObj = this.model.ObjectWithManyTypes.GetReference(id);

				dbObj.String = "boo";
				dbObj.NullableDateTime = DateTime.UtcNow + TimeSpan.FromDays(1);

				Assert.That(((IDataAccessObjectAdvanced)dbObj).GetChangedProperties().Count, Is.EqualTo(2));

				scope.Complete();
			}
		}
	}
}
