// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.MySql
{
	public class MySqlSqlQueryFormatter
		: Sql92QueryFormatter
	{
		private readonly bool silentlyIgnoreIndexConditions;

		public MySqlSqlQueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, TypeDescriptorProvider typeDescriptorProvider, bool silentlyIgnoreIndexConditions)
			: base(options, sqlDialect, sqlDataTypeProvider, typeDescriptorProvider)
		{
			this.silentlyIgnoreIndexConditions = silentlyIgnoreIndexConditions;
		}

		protected override Expression PreProcess(Expression expression)
		{
			expression = base.PreProcess(expression);
			expression = MySqlAutoIncrementAmender.Amend(expression);
			expression = MySqlInsertIntoAutoIncrementAmender.Amend(SqlReferencesColumnDeferrabilityRemover.Remove(expression));
			expression = MySqlNestedTableReferenceInUpdateFixer.Fix(expression);
			expression = MySqlDefaultValueConstraintFixer.Fix(expression);

			if (this.silentlyIgnoreIndexConditions)
			{
				expression = MySqlIndexConditionRemover.Remove(expression);
			}

			return expression;
		}

		protected override FunctionResolveResult ResolveSqlFunction(SqlFunctionCallExpression functionCallExpression)
		{
			var function = functionCallExpression.Function;
			var arguments = functionCallExpression.Arguments;

			switch (function)
			{
				case SqlFunction.TrimLeft:
					return new FunctionResolveResult("LTRIM", false, arguments);
				case SqlFunction.TrimRight:
					return new FunctionResolveResult("RTRIM", false, arguments);
				case SqlFunction.ServerUtcNow:
					return new FunctionResolveResult("UTC_TIMESTAMP", false, functionCallExpression.Arguments);
				case SqlFunction.TimeSpanFromSeconds:
					return new FunctionResolveResult("", false, arguments)
					{
						functionPrefix = "INTERVAL ",
						functionSuffix = " SECOND",
						excludeParenthesis = true
					};
				case SqlFunction.TimeSpanFromMinutes:
					return new FunctionResolveResult("", false, arguments[0])
					{
						functionPrefix = "INTERVAL ",
						functionSuffix = " MINUTE",
						excludeParenthesis = true
					};
				case SqlFunction.TimeSpanFromHours:
					return new FunctionResolveResult("", false, arguments[0])
					{
						functionPrefix = "INTERVAL ",
						functionSuffix = " HOUR",
						excludeParenthesis = true
					};
				case SqlFunction.TimeSpanFromDays:
					return new FunctionResolveResult("", false, arguments[0])
					{
						functionPrefix = "INTERVAL ",
						functionSuffix = " DAY",
						excludeParenthesis = true
					};
			case SqlFunction.DateTimeAddTimeSpan:
					return new FunctionResolveResult("DATE_ADD", false, arguments);
			}

			return base.ResolveSqlFunction(functionCallExpression);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			switch (functionCallExpression.Function)
			{
				case SqlFunction.DayOfWeek:
					Write("((");
					base.VisitFunctionCall(functionCallExpression);
					Write(") - 1)");

					return functionCallExpression;
			case SqlFunction.DateTimeAddDays:
				Write("(");
				Write("DATE_ADD(");
				Visit(functionCallExpression.Arguments[0]);
				Write(", INTERVAL ");
				Visit(functionCallExpression.Arguments[1]);
				Write(" DAY))");

				return functionCallExpression;
			case SqlFunction.DateTimeAddMonths:
				Write("(");
				Write("DATE_ADD(");
				Visit(functionCallExpression.Arguments[0]);
				Write(", INTERVAL ");
				Visit(functionCallExpression.Arguments[1]);
				Write(" MONTH))");

				return functionCallExpression;
			case SqlFunction.DateTimeAddYears:
				Write("(");
				Write("DATE_ADD(");
				Visit(functionCallExpression.Arguments[0]);
				Write(", INTERVAL ");
				Visit(functionCallExpression.Arguments[1]);
				Write(" YEAR))");

				return functionCallExpression;
			}

			return base.VisitFunctionCall(functionCallExpression);
		}

		protected override void WriteInsertIntoReturning(SqlInsertIntoExpression expression)
		{
			if (expression.ReturningAutoIncrementColumnNames == null
				|| expression.ReturningAutoIncrementColumnNames.Count == 0)
			{
				return;
			}
			
			Write("; SELECT LAST_INSERT_ID()");
		}

		protected override void WriteInsertDefaultValuesSuffix()
		{
			Write(" VALUES ()");
		}

		protected override void WriteDeferrability(SqlColumnReferenceDeferrability deferrability)
		{
		}
	}
}
