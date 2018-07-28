// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Shaolinq.Tests.SqlServerClusteredIndexes
{
	[TestFixture("SqlServer")]
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
			var expressions = this.model
				.GetCurrentSqlDatabaseContext()
				.SchemaManager
				.BuildDataDefinitonExpressions(DatabaseCreationOptions.DeleteExistingDatabase);

			var s = this.model
				.GetCurrentSqlDatabaseContext()
				.SqlQueryFormatterManager
				.Format(expressions);

			Assert.IsTrue(s.CommandText.Contains("CONSTRAINT \"pk_directory_id\" PRIMARY KEY NONCLUSTERED (\"DirectoryId\")"));
			Assert.IsTrue(s.CommandText.Contains("CONSTRAINT \"pk_administrator_id\" PRIMARY KEY NONCLUSTERED (\"AdministratorId\")"));
			Assert.IsTrue(s.CommandText.Contains("CREATE CLUSTERED INDEX \"idx_directory_name_id\" ON \"Directory\"(\"Name\", \"DirectoryId\");"));
			Assert.AreEqual(1, Regex.Matches(s.CommandText, "CREATE CLUSTERED INDEX").Count);
		}
	}
}
