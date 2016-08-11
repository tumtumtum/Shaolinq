// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
	public class DirectAccessTests
		: BaseTests<TestDataAccessModel>
	{
		public DirectAccessTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Create_Command()
		{
			using (var scope = new TransactionScope())
			{
				var transactionContext = scope.GetCurrentSqlTransactionalCommandsContext(this.model);

				using (var command = transactionContext.CreateCommand())
				{
					command.CommandText = "SELECT 1;";
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							Assert.AreEqual(1, reader.GetInt32(0));
						}
					}
				}
			}
		}
	}
}
