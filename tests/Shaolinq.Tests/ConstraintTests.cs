using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Sqlite", Category = "SupportsSqlite")]
	[TestFixture("SqliteInMemory", Category = "SupportsSqlite")]
	[TestFixture("SqliteClassicInMemory", Category = "SupportsSqlite")]
	public class ConstraintTests
			: BaseTests
	{
		public ConstraintTests(string providerName)
			: base(providerName)
		{
		}

		[Test, ExpectedException(typeof(UniqueKeyConstraintException))]
		public void Test_Unique_Non_PrimaryKey_Constraint()
		{
			using (var scope = new TransactionScope())
			{
				var obj1 = model.ObjectWithUniqueConstraints.Create();
				obj1.Name = "a";

				var obj2 = model.ObjectWithUniqueConstraints.Create();
				obj2.Name = "a";

				scope.Flush(model);

				scope.Complete();
			}
		}
	}
}
