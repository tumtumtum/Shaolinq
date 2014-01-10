using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Sqlite")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	public class DirectAccessTests
		: BaseTests
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
				var transactionContext = scope.GetCurrentSqlDataTransactionContext(model);

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
