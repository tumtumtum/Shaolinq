// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlQueryFormatter
		: Sql92QueryFormatter
	{
		private readonly Version version;
		private readonly int? typeSystemVersionNumber;
		private readonly string typeSystemVersion;
		private readonly bool uniqueNullIndexAnsiComplianceFixerClassicBehaviour;
		private readonly bool explicitIndexConditionOverridesNullAnsiCompliance;
		
		public SqlServerSqlQueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, TypeDescriptorProvider typeDescriptorProvider, bool uniqueNullIndexAnsiComplianceFixerClassicBehaviour, bool explicitIndexConditionOverridesNullAnsiCompliance, string typeSystemVersion, string serverVersion)
			: base(options, sqlDialect, sqlDataTypeProvider, typeDescriptorProvider)
		{
			this.uniqueNullIndexAnsiComplianceFixerClassicBehaviour = uniqueNullIndexAnsiComplianceFixerClassicBehaviour;
			this.explicitIndexConditionOverridesNullAnsiCompliance = explicitIndexConditionOverridesNullAnsiCompliance;
			this.typeSystemVersion = typeSystemVersion;

			Version.TryParse(serverVersion, out this.version);

			if (typeSystemVersion != null)
			{
				var match = Regex.Match(typeSystemVersion, "SQL Server [0-9]+", RegexOptions.IgnoreCase);

				if (match.Success)
				{
					this.typeSystemVersionNumber = Convert.ToInt32(match.Groups[1].Value);
				}
			}
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

		protected override Expression VisitConstraint(SqlConstraintExpression expression)
		{
			base.VisitConstraint(expression);

			if (!expression.AutoIncrement)
			{
				return expression;
			}

			var constraintOptions = expression.ConstraintOptions;

			if (constraintOptions != null && constraintOptions.Length == 2 && (constraintOptions[0] as long?) > 0)
			{
				if (constraintOptions[1] as long? == 0)
				{
					constraintOptions[1] = 1L;
				}

				Write("(");
				WriteDeliminatedListOfItems(constraintOptions, Write);
				Write(")");
			}

			return expression;
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			switch (functionCallExpression.Function)
			{
			case SqlFunction.Hour:
				Write("DATEPART(hour, ");
				Visit(functionCallExpression.Arguments[0]);
				Write(")");
				return functionCallExpression;
			case SqlFunction.Minute:
				Write("DATEPART(minute, ");
				Visit(functionCallExpression.Arguments[0]);
				Write(")");
				return functionCallExpression;
			case SqlFunction.Second:
				Write("DATEPART(second, ");
				Visit(functionCallExpression.Arguments[0]);
				Write(")");
				return functionCallExpression;
			case SqlFunction.DayOfWeek:
				Write("DATEPART(weekday, ");
				Visit(functionCallExpression.Arguments[0]);
				Write(")");
				return functionCallExpression;
			case SqlFunction.DayOfMonth:
				Write("DATEPART(day, ");
				Visit(functionCallExpression.Arguments[0]);
				Write(")");
				return functionCallExpression;
			case SqlFunction.DayOfYear:
				Write("DATEPART(dayofyear, ");
				Visit(functionCallExpression.Arguments[0]);
				Write(")");
				return functionCallExpression;
			case SqlFunction.Date:
				Write("CONVERT(date, ");
				Visit(functionCallExpression.Arguments[0]);
				Write(")");
				return functionCallExpression;
			case SqlFunction.DateTimeAddDays:
				Write("DATEADD(DAY, ");
				Visit(functionCallExpression.Arguments[1]);
				Write(", ");
				Visit(functionCallExpression.Arguments[0]);
				Write(")");
				return functionCallExpression;
			case SqlFunction.DateTimeAddMonths:
				Write("DATEADD(MONTH, ");
				Visit(functionCallExpression.Arguments[1]);
				Write(", ");
				Visit(functionCallExpression.Arguments[0]);
				Write(")");
				return functionCallExpression;
			case SqlFunction.DateTimeAddYears:
				Write("DATEADD(YEAR, ");
				Visit(functionCallExpression.Arguments[1]);
				Write(", ");
				Visit(functionCallExpression.Arguments[0]);
				Write(")");
				return functionCallExpression;
			case SqlFunction.DateTimeAddTimeSpan:
				Write("DATEADD(DAY, ");
				Write("CAST(");
				Visit(functionCallExpression.Arguments[1]);
				Write(" AS BIGINT)");
				Write(" / CAST(864000000000 AS BIGINT)");
				Write(", DATEADD(MS, ");
				Write("CAST(");
				Visit(functionCallExpression.Arguments[1]);
				Write(" AS BIGINT)");
				Write(" / CAST(10000 AS BIGINT) % 86400000, " );
				Visit(functionCallExpression.Arguments[0]);
				Write("))");
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
					Write("CAST(");
					Visit(unaryExpression.Operand);
					Write(" AS FLOAT)");

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

			var type = constantExpression.Value.GetType().GetUnwrappedNullableType();

			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Boolean:
				if ((this.options & SqlQueryFormatterOptions.EvaluateConstants) != 0)
				{
					Write(FormatConstant(Convert.ToInt32(constantExpression.Value)));
				}
				else
				{
					Write(this.ParameterIndicatorPrefix);
					Write(ParamNamePrefix);
					Write(this.parameterValues.Count);
					this.parameterValues.Add(new TypedValue(typeof(int), constantExpression.Value, c => Convert.ToInt32(c)));
				}

				return constantExpression;
			}

			return base.VisitConstant(constantExpression);
		}

		protected override Expression PreProcess(Expression expression)
		{
			expression = SqlServerClusteredIndexNormalizer.Normalize(expression);
			expression = SqlServerIdentityInsertAndUpdateAmender.Amend(this.typeDescriptorProvider, expression);
			expression = SqlServerSubqueryOrderByFixer.Fix(expression);

			if (this.version?.Major >= 12 || this.typeSystemVersionNumber >= 2012)
			{
				expression = SqlServerLimitEmulationAmender.Amend(expression);
			}
			else
			{
				expression = SqlServerLimitEmulationAmender.Amend(expression);
			}

			expression = SqlServerUniqueNullIndexAnsiComplianceFixer.Fix(expression, this.uniqueNullIndexAnsiComplianceFixerClassicBehaviour, this.explicitIndexConditionOverridesNullAnsiCompliance);
			expression = SqlServerDateTimeFunctionsAmender.Amend(expression);
			expression = SqlServerAggregateTypeFixer.Fix(expression);
			expression = SqlServerBooleanNormalizer.Normalize(expression);

			return base.PreProcess(expression);
		}

		protected override void AppendTop(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Take != null && selectExpression.Skip == null)
			{
				Write("TOP(");
				Visit(selectExpression.Take);
				Write(") ");
			}
		}

		protected override void AppendLimit(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null && selectExpression.Take != null)
			{
				Write(" OFFSET ");
				this.Visit(selectExpression.Skip);
				Write(" ROWS FETCH NEXT ");
				this.Visit(selectExpression.Take);
				Write(" ROWS");
			}
		}

		protected override void Write(SqlColumnReferenceAction action)
		{
			if (action == SqlColumnReferenceAction.Restrict)
			{
				Write("NO ACTION");

				return;
			}

			base.Write(action);
		}

		protected override Expression VisitOver(SqlOverExpression selectExpression)
		{
			Visit(selectExpression.Source);

			Write(" OVER (ORDER BY ");

			WriteDeliminatedListOfItems<Expression>(selectExpression.OrderBy, c =>
			{
				Visit(c);
			});

			Write(")");

			return selectExpression;
		}

		protected override bool WriteInsertIntoAfterSource(SqlInsertIntoExpression expression)
		{
			var tableHintExpression = expression.WithExpression as SqlTableHintExpression;

			if (tableHintExpression?.TableLock == true)
			{
				Write(" WITH (TABLOCK) ");
			}

			return true;
		}
		protected override void WriteInsertIntoReturning(SqlInsertIntoExpression expression)
		{
			if (expression.ReturningAutoIncrementColumnNames == null
				|| expression.ReturningAutoIncrementColumnNames.Count == 0)
			{
				return;
			}

			Write(" OUTPUT ");
			WriteDeliminatedListOfItems<string>(expression.ReturningAutoIncrementColumnNames, c =>
			{
				WriteQuotedIdentifier("INSERTED");
				Write(".");
				WriteQuotedIdentifier(c);
			}, ",");
			Write("");
		}

		protected override Expression VisitExtension(Expression expression)
		{
			if (expression is BitBooleanExpression booleanExpression)
			{
				Visit(booleanExpression.Expression);

				return expression;
			}

			if (expression is SqlTakeAllValueExpression sqlTakeAllValueExpression)
			{
				Write(Expression.Constant(Int64.MaxValue));

				return expression;
			}
			
			return base.VisitExtension(expression);
		}
	}
}
