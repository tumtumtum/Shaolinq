using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using NUnit.Framework;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Sqlite")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	public class SqlFormatterTests
		: BaseTests
	{
		public SqlFormatterTests(string providerName)
			: base(providerName)
		{	
		}

		[Test]
		public void Test_DataDefinitionBuilder()
		{
			var dbConnection = this.model.GetCurrentSqlDatabaseContext();
			var dataDefinitionExpressions = SqlDataDefinitionExpressionBuilder.Build(dbConnection.SqlDataTypeProvider, dbConnection.SqlDialect, this.model, string.Empty);

			var formatter = dbConnection.SqlQueryFormatterManager.CreateQueryFormatter();

			Console.WriteLine(formatter.Format(dataDefinitionExpressions).CommandText);
		}

		[Test]
		public void Test_Format_Create_Table_With_Table_Constraints()
		{
			var columnDefinitions = new List<Expression>
			{
				new SqlColumnDefinitionExpression("Column1", "INTEGER", new List<Expression> { new SqlSimpleConstraintExpression(SqlSimpleConstraint.Unique),  new SqlReferencesColumnExpression("Table2", SqlColumnReferenceDeferrability.InitiallyDeferred, new ReadOnlyCollection<string>(new [] { "Id"}), SqlColumnReferenceAction.NoAction, SqlColumnReferenceAction.SetNull)})
			};

			var constraints = new List<Expression>
			{
				new SqlSimpleConstraintExpression(SqlSimpleConstraint.Unique, new[] {"Column1"}),
				new SqlForeignKeyConstraintExpression("fkc", new ReadOnlyCollection<string>(new [] {"Column1"}), new SqlReferencesColumnExpression("Table2", SqlColumnReferenceDeferrability.InitiallyDeferred, new ReadOnlyCollection<string>(new [] { "Id"}), SqlColumnReferenceAction.NoAction, SqlColumnReferenceAction.NoAction))
			};

			var createTableExpression = new SqlCreateTableExpression(new SqlTableExpression("Table1"), false, columnDefinitions, constraints);

			var formatter = new Sql92QueryFormatter();

			Console.WriteLine(formatter.Format(createTableExpression).CommandText);
		}
	}
}
