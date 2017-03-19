// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	//[TestFixture("MySql")]
	//[TestFixture("Postgres")]
	//[TestFixture("Postgres.DotConnect")]
	//[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite", Category = "IgnoreOnMono")]
	//[TestFixture("SqliteInMemory")]
	//[TestFixture("SqliteClassicInMemory")]
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
			var dbConnection = this.model.GetCurrentSqlDatabaseContext();
			var dataDefinitionExpressions = SqlDataDefinitionExpressionBuilder.Build(dbConnection.SqlDataTypeProvider, dbConnection.SqlDialect, this.model, DatabaseCreationOptions.DeleteExistingDatabase, string.Empty, SqlDataDefinitionBuilderFlags.BuildTables | SqlDataDefinitionBuilderFlags.BuildIndexes);

			var formatter = dbConnection.SqlQueryFormatterManager.CreateQueryFormatter();

			Console.WriteLine(formatter.Format(dataDefinitionExpressions).CommandText);
		}

		[Test]
		public void Test_Format_Create_Table_With_Table_Constraints()
		{
			var columnDefinitions = new []
			{
				new SqlColumnDefinitionExpression("Column1", new SqlTypeExpression("INTEGER"), new List<SqlConstraintExpression> { new SqlConstraintExpression(SqlSimpleConstraint.Unique),  new SqlConstraintExpression(null, new SqlReferencesExpression(new SqlTableExpression("Table2"), SqlColumnReferenceDeferrability.InitiallyDeferred, new [] { "Id"}, SqlColumnReferenceAction.NoAction, SqlColumnReferenceAction.SetNull))})
			};

			var constraints = new []
			{
				new SqlConstraintExpression(SqlSimpleConstraint.Unique),
				new SqlConstraintExpression(new SqlReferencesExpression(new SqlTableExpression("Table2"), SqlColumnReferenceDeferrability.InitiallyDeferred, new [] { "Id"}, SqlColumnReferenceAction.NoAction, SqlColumnReferenceAction.NoAction), columnNames: new [] {"Column1"}, constraintName: "fck")
			};

			var createTableExpression = new SqlCreateTableExpression(new SqlTableExpression("Table1"), false, columnDefinitions.ToReadOnlyCollection(), constraints.ToReadOnlyCollection());

			var formatter = new Sql92QueryFormatter();

			Console.WriteLine(formatter.Format(createTableExpression).CommandText);
		}
	}
}
