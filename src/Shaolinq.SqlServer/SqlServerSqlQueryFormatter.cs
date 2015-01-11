using System;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlQueryFormatter
		: Sql92QueryFormatter
	{
		public SqlServerSqlQueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider)
			: base(options, sqlDialect, sqlDataTypeProvider)
		{
		}

		protected override void Write(SqlColumnReferenceAction action)
		{
			if (action == SqlColumnReferenceAction.Restrict)
			{
				this.Write("NO ACTION");

				return;
			}

			base.Write(action);
		}

		protected override void WriteInsertIntoReturning(SqlInsertIntoExpression expression)
		{
			if (expression.ReturningAutoIncrementColumnNames == null
				|| expression.ReturningAutoIncrementColumnNames.Count == 0)
			{
				return;
			}

			this.Write(" OUTPUT ");
			this.WriteDeliminatedListOfItems<string>(expression.ReturningAutoIncrementColumnNames, c =>
			{
				this.WriteQuotedIdentifier("INSERTED");
				this.Write(".");
				this.WriteQuotedIdentifier(c);

				return null;
			}, ",");
			this.Write("");
		}
	}
}
