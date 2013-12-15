// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
﻿using Shaolinq.Persistence.Sql;
﻿using Shaolinq.Persistence.Sql.Linq;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSqlQueryFormatter
		: Sql92QueryFormatter
	{
		public DataAccessModel DataAccessModel { get; private set; }

		protected override char ParameterIndicatorChar
		{
			get
			{
				return '@';
			}
		}

		public PostgresSqlQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
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
				this.Write("ROUND(CAST(");
				var c = Visit(column.Expression) as SqlColumnExpression;
				this.Write(" as NUMERIC)");
				this.Write(", 20)");

				if (!String.IsNullOrEmpty(column.Name))
				{
					this.Write(" AS ");
					this.Write(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.IdentifierQuote));
					this.Write(column.Name);
					this.Write(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.IdentifierQuote));
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
					this.Write(" LIMIT ");

					Visit(selectExpression.Take);
				}

				if (selectExpression.Skip != null)
				{
					this.Write(" OFFSET ");

					Visit(selectExpression.Skip);
				}
			}
		}
	}
}
