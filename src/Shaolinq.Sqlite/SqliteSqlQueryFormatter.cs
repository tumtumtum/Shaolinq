// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlQueryFormatter
		: Sql92QueryFormatter
	{
		public SqliteSqlQueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect,  SqlDataTypeProvider sqlDataTypeProvider)
			: base(options, sqlDialect, sqlDataTypeProvider)
		{
		}

		protected override Expression PreProcess(Expression expression)
		{
			IDictionary<string, string> primaryKeyNameByTablesWithReducedPrimaryKeyName;

			expression = base.PreProcess(expression);
			expression = SqliteAutoIncrementPrimaryKeyColumnReducer.Reduce(expression, out primaryKeyNameByTablesWithReducedPrimaryKeyName);
			expression = SqliteForeignKeyConstraintReducer.Reduce(expression, primaryKeyNameByTablesWithReducedPrimaryKeyName);

			return expression;
		}

		protected override void WriteDeferrability(SqlColumnReferenceDeferrability deferrability)
		{
			switch (deferrability)
			{
				case SqlColumnReferenceDeferrability.Deferrable:
					this.Write(" DEFERRABLE");
					break;
				case SqlColumnReferenceDeferrability.InitiallyDeferred:
					this.Write(" DEFERRABLE INITIALLY DEFERRED");
					break;
				case SqlColumnReferenceDeferrability.InitiallyImmediate:
					this.Write(" DEFERRABLE INITIALLY IMMEDIATE");
					break;
			}
		}

		protected override FunctionResolveResult ResolveSqlFunction(SqlFunctionCallExpression functionCallExpression)
		{
			var function = functionCallExpression.Function;
			var arguments = functionCallExpression.Arguments;

			switch (function)
			{
			case SqlFunction.ServerUtcNow:
				return new FunctionResolveResult("STRFTIME", false, FunctionResolveResult.MakeArguments("%Y-%m-%d %H:%M:%f0000", "now"), null, arguments);
			case SqlFunction.Concat:
				return new FunctionResolveResult("||", true, arguments);
			case SqlFunction.ServerNow:
				return new FunctionResolveResult("STRFTIME", false, FunctionResolveResult.MakeArguments("%Y-%m-%d %H:%M:%f0000", "now", "localtime"), null, arguments);
			case SqlFunction.TimeSpanFromSeconds:
				return new FunctionResolveResult("", false, new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, arguments[0], Expression.Constant(" seconds")))
				{
					excludeParenthesis = true
				};
			case SqlFunction.TimeSpanFromMinutes:
				return new FunctionResolveResult("", false, new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, arguments[0], Expression.Constant(" minutes")))
				{
					excludeParenthesis = true
				};
			case SqlFunction.TimeSpanFromHours:
				return new FunctionResolveResult("", false, new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, arguments[0], Expression.Constant(" hours")))
				{
					excludeParenthesis = true
				};
			case SqlFunction.TimeSpanFromDays:
				return new FunctionResolveResult("", false, new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, arguments[0], Expression.Constant(" days")))
				{
					excludeParenthesis = true
				};
			case SqlFunction.DateTimeAddTimeSpan:
				return new FunctionResolveResult("STRFTIME", false, Expression.Constant("%Y-%m-%d %H:%M:%f0000"), arguments[0], arguments[1]);
			case SqlFunction.Year:
				return new FunctionResolveResult("STRFTIME", false, FunctionResolveResult.MakeArguments("%Y"), null, arguments);
			case SqlFunction.Month:
				return new FunctionResolveResult("STRFTIME", false, FunctionResolveResult.MakeArguments("%m"), null, arguments);
			case SqlFunction.Week:
				return new FunctionResolveResult("STRFTIME", false, FunctionResolveResult.MakeArguments("%W"), null, arguments);
			case SqlFunction.DayOfYear:
				return new FunctionResolveResult("STRFTIME", false, FunctionResolveResult.MakeArguments("%j"), null, arguments);
			case SqlFunction.DayOfMonth:
				return new FunctionResolveResult("STRFTIME", false, FunctionResolveResult.MakeArguments("%d"), null, arguments);
			case SqlFunction.DayOfWeek:
				return new FunctionResolveResult("STRFTIME", false, FunctionResolveResult.MakeArguments("%w"), null, arguments);
			case SqlFunction.Substring:
				return new FunctionResolveResult("SUBSTR", false, arguments);
			case SqlFunction.TrimLeft:
				return new FunctionResolveResult("LTRIM", false, arguments);
			case SqlFunction.TrimRight:
				return new FunctionResolveResult("RTRIM", false, arguments);
			case SqlFunction.StringLength:
				return new FunctionResolveResult("LENGTH", false, arguments);
			}

			return base.ResolveSqlFunction(functionCallExpression);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (functionCallExpression.Function >= SqlFunction.NumberBasedDatePartStart
				&& functionCallExpression.Function <= SqlFunction.NumberBasedDatePartEnd)
			{
				this.Write("(CAST (");
				base.VisitFunctionCall(functionCallExpression);
				this.Write(" AS INTEGER))");

				return functionCallExpression;
			}

			return base.VisitFunctionCall(functionCallExpression);
		}


		protected override void AppendLimit(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null || selectExpression.Take != null)
			{
				if (selectExpression.Take != null)
				{
					this.Write(" LIMIT ");

					this.Visit(selectExpression.Take);

					this.Write(" ");
				}
				else
				{
					this.Write(" LIMIT -1 ");
				}

				if (selectExpression.Skip != null)
				{
					this.Write("OFFSET ");

					this.Visit(selectExpression.Skip);
				}
			}
		}

		protected override void WriteInsertIntoReturning(SqlInsertIntoExpression expression)
		{
			if (expression.ReturningAutoIncrementColumnNames == null
				|| expression.ReturningAutoIncrementColumnNames.Count == 0)
			{
				return;
			}

			this.Write("; SELECT last_insert_rowid()");
		}

		protected override void WriteInsertDefaultValuesSuffix()
		{
			this.Write(" DEFAULT VALUES");
		}
	}
}
