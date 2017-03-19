// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	/*[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite")]*/
	//[TestFixture("SqlServer", Category = "IgnoreOnMono")]
	/*[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]*/
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
