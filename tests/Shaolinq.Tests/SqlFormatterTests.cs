// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite", Category = "IgnoreOnMono")]
	[TestFixture("SqliteInMemory", Category = "IgnoreOnMono")]
	[TestFixture("SqliteClassicInMemory", Category = "IgnoreOnMono")]
	public class SqlFormatterTests
		: BaseTests<TestDataAccessModel>
	{
		public SqlFormatterTests(string providerName)
			: base(providerName)
		{	
		}

		[Test]
		public void Test_ServerSqlDataDefinitionBuilder()
		{
			using (var scope = new TransactionScope())
			{
				var builder = this.model.GetCurrentSqlDatabaseContext().SchemaManager.ServerSqlDataDefinitionExpressionBuilder;

				builder.Build();
			}
		}

		[Test]
		public void Test_DataDefinitionBuilder()
		{
			var databaseContext = this.model.GetCurrentSqlDatabaseContext();
			var dataDefinitionExpressions = SqlDataDefinitionExpressionBuilder.Build(databaseContext.DataAccessModel, databaseContext.SqlQueryFormatterManager, databaseContext.SqlDataTypeProvider, databaseContext.SqlDialect, this.model, DatabaseCreationOptions.DeleteExistingDatabase, string.Empty, SqlDataDefinitionBuilderFlags.BuildTables | SqlDataDefinitionBuilderFlags.BuildIndexes);

			var formatter = databaseContext.SqlQueryFormatterManager.CreateQueryFormatter();

			Console.WriteLine(formatter.Format(dataDefinitionExpressions).CommandText);
		}

		[Test]
		public void Test_Format_Create_Table_With_Table_Constraints()
		{
			var columnDefinitions = new []
			{
				new SqlColumnDefinitionExpression("Column1", new SqlTypeExpression("INTEGER"), new List<SqlConstraintExpression> { new SqlConstraintExpression(ConstraintType.Unique),  new SqlConstraintExpression(new SqlReferencesExpression(new SqlTableExpression("Table2"), SqlColumnReferenceDeferrability.InitiallyDeferred, new [] { "Id"}, SqlColumnReferenceAction.NoAction, SqlColumnReferenceAction.SetNull))})
			};

			var constraints = new []
			{
				new SqlConstraintExpression(ConstraintType.Unique),
				new SqlConstraintExpression(new SqlReferencesExpression(new SqlTableExpression("Table2"), SqlColumnReferenceDeferrability.InitiallyDeferred, new [] { "Id"}, SqlColumnReferenceAction.NoAction, SqlColumnReferenceAction.NoAction), columnNames: new [] {"Column1"}, constraintName: "fck")
			};

			var createTableExpression = new SqlCreateTableExpression(new SqlTableExpression("Table1"), false, columnDefinitions.ToReadOnlyCollection(), constraints.ToReadOnlyCollection(), null);

			var formatter = new Sql92QueryFormatter();

			Console.WriteLine(formatter.Format(createTableExpression).CommandText);
		}
	}
}
