// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Postgres.Devart
{
	public class DevartSqlQueryFormatter
		: Sql92QueryFormatter
	{
		public BaseDataAccessModel DataAccessModel { get; private set; }

		protected override char ParameterIndicatorChar
		{
			get
			{
				return '@';
			}
		}

		public DevartSqlQueryFormatter(BaseDataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
			: base(expression, options, sqlDataTypeProvider, sqlDialect)
		{
			this.DataAccessModel = dataAccessModel;
		}

		protected override FunctionResolveResult ResolveSqlFunction(SqlFunction function, ReadOnlyCollection<Expression> arguments)
		{
			switch (function)
			{
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
			}

			return base.ResolveSqlFunction(function, arguments);
		}

		protected override void VisitColumn(SqlSelectExpression selectExpression, SqlColumnDeclaration column)
		{
			if (column.Expression.Type == typeof(Decimal))
			{
				commandText.Append("ROUND(CAST(");
				var c = Visit(column.Expression) as SqlColumnExpression;
				commandText.Append(" as NUMERIC)");
				commandText.Append(", 20)");

				if (!String.IsNullOrEmpty(column.Name))
				{
					commandText.Append(" AS ");
					commandText.Append(this.sqlDialect.NameQuoteChar)
						.Append(column.Name)
						.Append(this.sqlDialect.NameQuoteChar);
				}
			}
			else
			{
				base.VisitColumn(selectExpression, column);
			}
		}

	    protected override void AppendLimit(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null || selectExpression.Take != null)
			{
				if (selectExpression.Take != null)
				{
					commandText.Append(" LIMIT ");

					Visit(selectExpression.Take);
				}

				if (selectExpression.Skip != null)
				{
					commandText.Append(" OFFSET ");

					Visit(selectExpression.Skip);
				}
			}
		}
	}
}
