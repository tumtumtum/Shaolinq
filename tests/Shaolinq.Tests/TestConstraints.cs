// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	public class TestConstraints
		: BaseTests
	{
		public TestConstraints(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Size_Constraint_Ok()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();
				var student = school.Students.Create();

				student.Email = new string('A', 63);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();
				var student = school.Students.Create();

				student.Email = new string('A', 64);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Size_Constraint_NotOk()
		{
			if (this.ProviderName == "Sqlite")
			{
				return;
			}

			Assert.Throws<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var school = this.model.Schools.Create();
					var student = school.Students.Create();

					student.Email = new string('A', 65);

					scope.Complete();
				}
			});
		}
	}
}
