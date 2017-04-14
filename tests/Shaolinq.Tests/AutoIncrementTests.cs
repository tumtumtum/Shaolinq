// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class AutoIncrementTests
		: BaseTests<TestDataAccessModel>
	{
		public AutoIncrementTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test()
		{
			using (var scope = new TransactionScope())
			{
			}
		}
	}
}
