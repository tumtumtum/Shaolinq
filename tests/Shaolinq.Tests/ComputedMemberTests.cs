// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
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
			
			using (var scope = new DataAccessScope())
			{
				cat = this.model.Cats.Create();

				scope.Save();

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
