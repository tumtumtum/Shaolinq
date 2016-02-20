// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Transactions;
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
		public void Test()
		{
			using (var scope = new TransactionScope())
			{
				var cat = this.model.Cats.Create();

				scope.Flush();

				scope.Complete();
			}
		}
	}
}
