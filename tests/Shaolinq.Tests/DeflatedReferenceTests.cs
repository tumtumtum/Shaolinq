using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.DataAccessModel.Test;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class DeflatedReferenceTests
		: BaseTests
	{
		public DeflatedReferenceTests(string providerName)
			: base(providerName)
		{	
		}

		[Test]
		public void Test_Get_Deflated_Reference_From_Object_With_Non_AutoIncrementing_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongNonAutoIncrementPrimaryKeys.NewDataAccessObject();

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
				var school = model.Schools.NewDataAccessObject();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.NewDataAccessObject();

				student.Birthdate = new DateTime(1940, 11, 27);
				student.FirstName = "Bruce";
				student.LastName = "Lee";

				var friend = school.Students.NewDataAccessObject();
				
				friend.FirstName = "Chuck";
				friend.LastName = "Norris";

				student.BestFriend = friend;

				scope.Flush(model);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.Students.First(c => c.FirstName == "Bruce");

				Assert.IsTrue(student.BestFriend.IsDeflatedReference);
				Assert.AreEqual("Chuck", student.BestFriend.FirstName);
				Assert.IsFalse(student.BestFriend.IsDeflatedReference);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Student_Then_Access_School_As_DeflatedReference()
		{
			Guid studentId;

			using (var scope = new TransactionScope())
			{
				var school = model.Schools.NewDataAccessObject();

				school.Name = "The Shaolinq School of Kung Fu";

				var student = school.Students.NewDataAccessObject();

				student.Birthdate = new DateTime(1940, 11, 27);
				student.FirstName = "Bruce";
				student.LastName = "Lee";

				scope.Flush(model);

				studentId = student.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.GetReferenceByPrimaryKey<Student>(new { Id = studentId });

				Assert.IsTrue(student.IsDeflatedReference);

				Assert.AreEqual("Bruce", student.FirstName);

				Assert.IsFalse(student.IsDeflatedReference);

				Assert.AreEqual("Bruce", student.FirstName);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.GetReferenceByPrimaryKey<Student>(studentId);

				Assert.IsTrue(student.IsDeflatedReference);

				Assert.AreEqual("Bruce", student.FirstName);

				Assert.IsFalse(student.IsDeflatedReference);

				Assert.AreEqual("Bruce", student.FirstName);

				scope.Complete();
			}


			using (var scope = new TransactionScope())
			{
				var student = this.model.GetReferenceByPrimaryKey<Student>(studentId);

				Assert.IsTrue(student.IsDeflatedReference);

				var sameStudent = this.model.Students.First(c => c.Id == studentId);

				Assert.IsFalse(student.IsDeflatedReference);

				Assert.AreSame(student, sameStudent);
				Assert.AreEqual("Bruce", student.FirstName);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = this.model.GetReferenceByPrimaryKey<Student>(studentId);

				Assert.IsTrue(student.IsDeflatedReference);

				student.Inflate();

				Assert.IsFalse(student.IsDeflatedReference);

				Assert.AreEqual("Bruce", student.FirstName);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = model.Students.FirstOrDefault();

				Assert.AreEqual(1, student.School.Id);
				Assert.IsTrue(student.School.IsDeflatedReference);

				Assert.AreEqual("The Shaolinq School of Kung Fu", student.School.Name);

				Assert.IsFalse(student.School.IsDeflatedReference);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = model.Students.FirstOrDefault();

				Assert.AreEqual(1, student.School.Id);
				Assert.IsTrue(student.School.IsDeflatedReference);

				var school = model.Schools.FirstOrDefault();

				Assert.IsFalse(student.School.IsDeflatedReference);

				Assert.AreEqual(school, student.School);

				scope.Complete();
			}
		}
	}
}
