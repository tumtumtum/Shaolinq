using NUnit.Framework;

namespace Shaolinq.Tests.SqlServerClusteredIndexes
{
	[TestFixture("SqlServer", "IgnoreOnMono")]
	public class SqlServerClusteredIndexesTest
		: BaseTests<SqlServerDataAccessModel>
	{
		public SqlServerClusteredIndexesTest(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test()
		{
		}
	}
}
