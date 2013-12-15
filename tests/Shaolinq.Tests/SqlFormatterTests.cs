using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NUnit.Framework;
using Shaolinq.Persistence.Sql.Linq;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class SqlFormatterTests
	{
		[Test]
		public void Test_Format_Create_Table_With_Table_Constraints()
		{
			var columnDefinitions = new List<Expression>
			{
				new SqlColumnDefinitionExpression("Column1", "INTEGER", new List<Expression> { new SqlSimpleConstraintExpression(SqlSimpleConstraint.Unique),  new SqlReferencesColumnExpression("Table2", SqlColumnReferenceDeferrability.InitiallyDeferred, new [] { "Id"}, SqlColumnReferenceAction.NoAction, SqlColumnReferenceAction.SetNull)})
			};

			var constraints = new List<Expression>
			{
				new SqlSimpleConstraintExpression(SqlSimpleConstraint.Unique, new[] {"Column1"}),
				new SqlForeignKeyConstraintExpression(new [] {"Column1"}, new SqlReferencesColumnExpression("Table2", SqlColumnReferenceDeferrability.InitiallyDeferred, new [] { "Id"}, SqlColumnReferenceAction.NoAction, SqlColumnReferenceAction.NoAction))
			};

			var createTableExpression = new SqlCreateTableExpression("Table1", columnDefinitions, constraints);

			var formatter = new Sql92QueryFormatter(createTableExpression);

			Console.WriteLine(formatter.Format().CommandText);
		}
	}
}
