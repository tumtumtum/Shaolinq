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

		protected override Expression VisitSetCommand(SqlSetCommandExpression expression)
		{
			this.Write("SET ");
			switch (expression.ConfigurationParameter)
			{
			case "IdentityInsert":
				this.Write("IDENTITY_INSERT");
				break;
			default:
				this.Write(expression.ConfigurationParameter);
				break;
			}
			
			if (expression.Target != null)
			{
				this.Write(" ");
				this.Write(((SqlTableExpression)expression.Target).Name);
				this.Write(" ");
			}

			if (expression.ConfigurationParameter == "IdentityInsert")
			{
				this.Write((bool)((ConstantExpression)expression.Arguments[0]).Value ? "ON" : "OFF");
			}
			else
			{
				this.Write(" ");
				this.Write(expression.Arguments);
			}

			this.WriteLine();

			return expression;
		}
	}
}
