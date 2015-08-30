// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.ObjectModel;
using System.Linq.Expressions;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.MySql
{
	public class MySqlSqlQueryFormatter
		: Sql92QueryFormatter
	{
		public MySqlSqlQueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider)
			: base(options, sqlDialect, sqlDataTypeProvider)
		{
		}

		protected override Expression PreProcess(Expression expression)
		{
			expression = base.PreProcess(expression);
			expression = MySqlInsertIntoAutoIncrementAmmender.Ammend(SqlReferencesColumnDeferrabilityRemover.Remove(expression), this.sqlDataTypeProvider);

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
					this.Write("((");
					base.VisitFunctionCall(functionCallExpression);
					this.Write(") - 1)");

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
			
			this.Write("; SELECT LAST_INSERT_ID()");
		}

		protected override void WriteInsertDefaultValuesSuffix()
		{
			this.Write(" VALUES ()");
		}

		protected override void WriteDeferrability(SqlColumnReferenceDeferrability deferrability)
		{
		}
	}
}
