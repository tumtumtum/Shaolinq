// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Postgres
{
	public class PostgresSqlQueryFormatter
		: Sql92QueryFormatter
	{
		private int selectNesting = 0;
		private readonly string schemaName;
		internal bool ConvertEnumsToText { get; }

		public PostgresSqlQueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, TypeDescriptorProvider typeDescriptorProvider, string schemaName, bool convertEnumsToText)
			: base(options, sqlDialect, sqlDataTypeProvider, typeDescriptorProvider)
		{
			this.schemaName = schemaName;
			this.ConvertEnumsToText = convertEnumsToText;
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			this.selectNesting++;

			var retval = base.VisitSelect(selectExpression);

			this.selectNesting--;

			return retval;
		}

		protected override void Write(SqlJoinType joinType)
		{
			switch (joinType)
			{
			case SqlJoinType.CrossApply:
				this.Write(" CROSS JOIN LATERAL ");
				break;
			case SqlJoinType.OuterApply:
				this.Write(" OUTER JOIN LATERAL ");
				break;
			default:
				base.Write(joinType);
				break;
			}
		}
		
		protected override Expression PreProcess(Expression expression)
		{
			expression =  PostgresDataDefinitionExpressionAmender.Amend(base.PreProcess(expression), this.sqlDataTypeProvider);

			return expression;
		}

		protected override Expression VisitOrderBy(SqlOrderByExpression orderByExpression)
		{
			base.VisitOrderBy(orderByExpression);

			switch (orderByExpression.OrderType)
			{
			case OrderType.Ascending:
				this.Write(" NULLS FIRST");
				break;
			default:
				this.Write(" NULLS LAST");
				break;
			}

			return orderByExpression;
		}

		protected override FunctionResolveResult ResolveSqlFunction(SqlFunctionCallExpression functionCallExpression)
		{
			var function = functionCallExpression.Function;
			var arguments = functionCallExpression.Arguments;

			switch (function)
			{
			case SqlFunction.ServerUtcNow:
				return new FunctionResolveResult("(NOW() at time zone 'utc')", false, arguments)
				{
					excludeParenthesis = true
				};
			case SqlFunction.TimeSpanFromSeconds:
				return new FunctionResolveResult("", true, new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, Expression.Call(arguments[0], MethodInfoFastRef.ObjectToStringMethod), Expression.Constant(" seconds")))
				{
					functionSuffix = "::interval"
				};
			case SqlFunction.TimeSpanFromMinutes:
				return new FunctionResolveResult("", true, new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, Expression.Call(arguments[0], MethodInfoFastRef.ObjectToStringMethod), Expression.Constant(" minutes")))
				{
					functionSuffix = "::interval"
				};
			case SqlFunction.TimeSpanFromHours:
				return new FunctionResolveResult("", true, new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, Expression.Call(arguments[0], MethodInfoFastRef.ObjectToStringMethod), Expression.Constant(" hours")))
				{
					functionSuffix = "::interval"
				};
			case SqlFunction.TimeSpanFromDays:
				return new FunctionResolveResult("", true, new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, Expression.Call(arguments[0], MethodInfoFastRef.ObjectToStringMethod), Expression.Constant(" days")))
				{
					functionSuffix = "::interval"
				};
			case SqlFunction.DateTimeAddTimeSpan:
				return new FunctionResolveResult("+", true, arguments);
			case SqlFunction.Concat:
				return new FunctionResolveResult("||", true, arguments);
			case SqlFunction.TrimLeft:
				return new FunctionResolveResult("LTRIM", false, arguments);
			case SqlFunction.TrimRight:
				return new FunctionResolveResult("RTRIM", false, arguments);
			case SqlFunction.Round:
				return new FunctionResolveResult("ROUND", false, arguments);
			case SqlFunction.DayOfMonth:
				return new FunctionResolveResult("date_part", false, FunctionResolveResult.MakeArguments("DAY"), null, arguments);
			case SqlFunction.DayOfWeek:
				return new FunctionResolveResult("date_part", false, FunctionResolveResult.MakeArguments("DOW"), null, arguments);
			case SqlFunction.DayOfYear:
				return new FunctionResolveResult("date_part", false, FunctionResolveResult.MakeArguments("DOY"), null, arguments);
			case SqlFunction.Year:
				return new FunctionResolveResult("date_part", false, FunctionResolveResult.MakeArguments("YEAR"), null, arguments);
			case SqlFunction.Month:
				return new FunctionResolveResult("date_part", false, FunctionResolveResult.MakeArguments("MONTH"), null, arguments);
			case SqlFunction.Hour:
				return new FunctionResolveResult("date_part", false, FunctionResolveResult.MakeArguments("HOUR"), null, arguments);
			case SqlFunction.Second:
				return new FunctionResolveResult("date_part", false, FunctionResolveResult.MakeArguments("SECOND"), null, arguments);
			case SqlFunction.Minute:
				return new FunctionResolveResult("date_part", false, FunctionResolveResult.MakeArguments("MINUTE"), null, arguments);
			case SqlFunction.Week:
				return new FunctionResolveResult("date_part", false, FunctionResolveResult.MakeArguments("WEEK"), null, arguments);
			case SqlFunction.StringLength:
				return new FunctionResolveResult("char_length", false, arguments);
			}

			return base.ResolveSqlFunction(functionCallExpression);
		}

		protected override void VisitColumn(SqlSelectExpression selectExpression, SqlColumnDeclaration column)
		{
			if (column.Expression.Type == typeof(Decimal))
			{
				this.Write("ROUND(CAST(");
				var c = this.Visit(column.Expression) as SqlColumnExpression;
				this.Write(" as NUMERIC)");
				this.Write(", 20)");

				if (!String.IsNullOrEmpty(column.Name))
				{
					this.Write(" AS ");
					this.Write(this.identifierQuoteString);
					this.Write(column.Name);
					this.Write(this.identifierQuoteString);
				}
			}
			else
			{
				base.VisitColumn(selectExpression, column);
			}
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (this.selectNesting == 1 && (this.ConvertEnumsToText && columnExpression.Type.GetUnwrappedNullableType().IsEnum))
			{
				base.VisitColumn(columnExpression);
				this.Write("::TEXT");

				return columnExpression;
			}

			return base.VisitColumn(columnExpression);
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (this.ConvertEnumsToText && constantExpression.Type.GetUnwrappedNullableType().IsEnum)
			{
				base.VisitConstant(constantExpression);
				this.Write("::TEXT");

				return constantExpression;
			}

			return base.VisitConstant(constantExpression);
		}

		protected override void AppendLimit(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null || selectExpression.Take != null)
			{
				if (selectExpression.Take != null)
				{
					this.Write(" LIMIT ");

					this.Visit(selectExpression.Take);
				}

				if (selectExpression.Skip != null)
				{
					this.Write(" OFFSET ");

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

			this.Write(" RETURNING ");
			this.WriteDeliminatedListOfItems<string>(expression.ReturningAutoIncrementColumnNames,this.WriteQuotedIdentifier, ",");
		}

		public override void AppendFullyQualifiedQuotedTableOrTypeName(string tableName, Action<string> append)
		{
			if (!string.IsNullOrEmpty(this.schemaName))
			{
				append(this.identifierQuoteString);
				append(this.schemaName);
				append(this.identifierQuoteString);
				append(".");
			}

			append(this.identifierQuoteString);
			append(tableName);
			append(this.identifierQuoteString);
		}


		protected override Expression VisitIndexedColumn(SqlIndexedColumnExpression indexedColumnExpression)
		{
			if (indexedColumnExpression.LowercaseIndex)
			{
				this.Write("(lower(");
			}

			this.Visit(indexedColumnExpression.Column);

			if (indexedColumnExpression.LowercaseIndex)
			{
				this.Write("))");
			}

			switch (indexedColumnExpression.SortOrder)
			{
			case SortOrder.Descending:
			this.Write(" DESC");
			break;
			case SortOrder.Ascending:
			this.Write(" ASC");
			break;
			case SortOrder.Unspecified:
			break;
			}

			return indexedColumnExpression;
		}
	}
}
