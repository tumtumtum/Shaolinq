// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite")]
	[TestFixture("Sqlite:DataAccessScope")]
	[TestFixture("SqlServer", Category = "IgnoreOnMono")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	public class ComputedMemberTests
		: BaseTests<TestDataAccessModel>
	{
		public ComputedMemberTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_GetReference()
		{
			Cat cat;
			long id;
			
			using (var scope = this.NewTransactionScope())
			{
				cat = this.model.Cats.Create();

				scope.Flush();

				id = cat.Id;

				Assert.AreEqual(cat.Id + 100000000, cat.MutatedId);

				var cat2 = this.model.Cats.GetReference(new { MutatedId = cat.Id + 100000000 });

				Assert.AreSame(cat, cat2);

				scope.Complete();
			}

			var cat3 = this.model.Cats.GetReference(new { MutatedId = id + 100000000 });

			Assert.AreNotSame(cat, cat3);
			Assert.AreEqual(id, cat3.Id);
			Assert.AreEqual(id, cat3.Id);
		}
	}
}
