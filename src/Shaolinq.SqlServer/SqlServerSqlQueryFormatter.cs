// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Platform;
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

		protected override FunctionResolveResult ResolveSqlFunction(SqlFunctionCallExpression functionCallExpression)
		{
			var function = functionCallExpression.Function;
			var arguments = functionCallExpression.Arguments;

			switch (function)
			{
			case SqlFunction.ServerUtcNow:
				return new FunctionResolveResult("SYSDATETIME", false, arguments);
			case SqlFunction.ServerNow:
				return new FunctionResolveResult("SYSUTCDATETIME", false, arguments);
			case SqlFunction.Substring:
				if (arguments.Count == 2)
				{
					return new FunctionResolveResult("SUBSTRING", false, arguments.Concat(Expression.Constant(Int32.MaxValue)).ToReadOnlyCollection());
				}
				else
				{
					return new FunctionResolveResult("SUBSTRING", false, arguments);
				}
			case SqlFunction.StringLength:
				return new FunctionResolveResult("LEN", false, arguments);
			}

			return base.ResolveSqlFunction(functionCallExpression);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			switch (functionCallExpression.Function)
			{
			case SqlFunction.Date:
				this.Write("CONVERT(date, ");
				this.Visit(functionCallExpression.Arguments[0]);
				this.Write(")");
				return functionCallExpression;
			case SqlFunction.DateTimeAddTimeSpan:
				this.Write("DATEADD(DAY, ");
				this.Write("CAST(");
				this.Visit(functionCallExpression.Arguments[1]);
				this.Write(" AS BIGINT)");
				this.Write(" / CAST(864000000000 AS BIGINT)");
				this.Write(", DATEADD(MS, ");
				this.Write("CAST(");
				this.Visit(functionCallExpression.Arguments[1]);
				this.Write(" AS BIGINT)");
				this.Write(" / CAST(10000 AS BIGINT) % 86400000, " );
				this.Visit(functionCallExpression.Arguments[0]);
				this.Write("))");
				return functionCallExpression;
			}

			return base.VisitFunctionCall(functionCallExpression);
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			switch (unaryExpression.NodeType)
			{
			case ExpressionType.Convert:
				if (unaryExpression.Type == typeof(double))
				{
					this.Write("CAST(");
					this.Visit(unaryExpression.Operand);
					this.Write(" AS FLOAT)");

					return unaryExpression;
				}
				break;
			}

			return base.VisitUnary(unaryExpression);
		}


		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (constantExpression.Value == null)
			{
				return base.VisitConstant(constantExpression);
			}

			var type = constantExpression.Value.GetType();

			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Boolean:
				if (Convert.ToBoolean(constantExpression.Value))
				{
					this.Write(this.ParameterIndicatorPrefix);
					this.Write(ParamNamePrefix);
					this.Write(this.parameterValues.Count);
					this.parameterValues.Add(new TypedValue(typeof(int), 1));

					return constantExpression;
				}
				else
				{
					this.Write(this.ParameterIndicatorPrefix);
					this.Write(ParamNamePrefix);
					this.Write(this.parameterValues.Count);
					this.parameterValues.Add(new TypedValue(typeof(int), 0));

					return constantExpression;
				}
			}

			return base.VisitConstant(constantExpression);
		}

		protected override Expression PreProcess(Expression expression)
		{
			expression = SqlServerSubqueryOrderByFixer.Fix(expression);
			expression = SqlServerLimitAmender.Amend(expression);
			expression = SqlServerBooleanNormalizer.Normalize(expression);
			expression = SqlServerDateTimeFunctionsAmender.Amend(expression);

			return base.PreProcess(expression);
		}

		protected override void AppendTop(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Take != null && selectExpression.Skip == null)
			{
				this.Write("TOP(");
				this.Visit(selectExpression.Take);
				this.Write(") ");
			}
		}

		protected override void AppendLimit(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null && selectExpression.Take != null)
			{
				throw new InvalidOperationException("Skip/Take not supported");
			}
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

		protected override Expression VisitOver(SqlOverExpression selectExpression)
		{
			this.Visit(selectExpression.Source);

			this.Write(" OVER (ORDER BY ");

			this.WriteDeliminatedListOfItems<Expression>(selectExpression.OrderBy, c =>
			{
				this.Visit(c);

				if (((SqlOrderByExpression)c).OrderType == OrderType.Descending)
				{
					this.Write(" DESC");
				}
			});

			this.Write(")");

			return selectExpression;
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
				this.Write((bool)((ConstantExpression)expression.Arguments[0].Reduce()).Value ? "ON" : "OFF");
			}
			else
			{
				this.Write(" ");
				this.Write(expression.Arguments);
			}

			this.WriteLine();

			return expression;
		}

		protected override Expression VisitExtension(Expression expression)
		{
			var booleanExpression = expression as BitBooleanExpression;

			if (booleanExpression != null)
			{
				this.Visit(booleanExpression.Expression);

				return expression;
			}

			var sqlTakeAllValueExpression = expression as SqlTakeAllValueExpression;

			if (sqlTakeAllValueExpression != null)
			{
				this.Write(Expression.Constant(Int64.MaxValue));

				return expression;
			}
			
			return base.VisitExtension(expression);
		}
	}
}
