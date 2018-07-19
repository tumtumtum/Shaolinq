// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Reflection;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;
using Shaolinq.DirectAccess.Sql;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("SqlServer")]
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
		public void Test_Execute_Reader()
		{
			var p = this.model.GetCurrentSqlDialect().GetSyntaxSymbolString(Persistence.SqlSyntaxSymbol.ParameterPrefix);
			var q = this.model.GetCurrentSqlDialect().GetSyntaxSymbolString(Persistence.SqlSyntaxSymbol.IdentifierQuote);

			using (var scope = new TransactionScope())
			{
				var count = 0;

				var school = this.model.Schools.Create();

				school.Name = MethodBase.GetCurrentMethod().Name + " Test School";

				var school2 = this.model.Schools.Create();

				school2.Name = MethodBase.GetCurrentMethod().Name + " Test School2";

				scope.Flush();

				foreach (var reader in this.model.ExecuteReader($"SELECT * FROM {q}School{q} WHERE {q}Name{q}={p}name", new { name = school.Name }))
				{
					count++;
				}

				Assert.AreEqual(1, count);

				Assert.AreEqual(count, this.model.Schools.Count(c => c.Name == school.Name));

				var schools = this.model.ExecuteReadAll(c => c.GetString(c.GetOrdinal("Name")), $"SELECT * FROM {q}School{q}");

				Assert.Greater(schools.Count, 1);
			}
		}

		[Test]
		public void Test_Create_Command_Using_Scope()
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

		[Test]
		public void Test_Create_Command()
		{
			using (var scope = new TransactionScope())
			{
				using (var command = this.model.CreateCommand())
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

		[Test]
		public void Test_Create_Command_Without_Scope()
		{
			Assert.Throws<InvalidOperationException>
			(() =>
			{
				using (var command = this.model.CreateCommand())
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
			});
		}
	}
}
