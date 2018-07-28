// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using Platform;
using Platform.Reflection;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class Sql92QueryFormatter
		: SqlQueryFormatter
	{
		public struct FunctionResolveResult
		{
			public static TypedValue[] MakeArguments(params object[] args)
			{
				var retval = new TypedValue[args.Length];

				for (var i = 0; i < args.Length; i++)
				{
					retval[i] = new TypedValue(args[i].GetType(), args[i]);
				}

				return retval;
			}

			public string functionName;
			public bool treatAsOperator;
			public string functionPrefix;
			public string functionSuffix;
			public TypedValue[] argsAfter;
			public TypedValue[] argsBefore;
			public IReadOnlyList<Expression> arguments;
			public bool excludeParenthesis;

			public FunctionResolveResult(string functionName, bool treatAsOperator, params Expression[] arguments)
				: this(functionName, treatAsOperator, null, null, arguments.ToReadOnlyCollection())
			{
			}

			public FunctionResolveResult(string functionName, bool treatAsOperator, IReadOnlyList<Expression> arguments)
				: this(functionName, treatAsOperator, null, null, arguments)
			{
			}

			public FunctionResolveResult(string functionName, bool treatAsOperator, TypedValue[] argsBefore, TypedValue[] argsAfter, IReadOnlyList<Expression> arguments)
			{
				this.functionPrefix = null;
				this.functionSuffix = null;
				this.functionName = functionName;
				this.treatAsOperator = treatAsOperator;
				this.argsBefore = argsBefore;
				this.argsAfter = argsAfter;
				this.arguments = arguments;
				this.excludeParenthesis = false;
			}
		}

		protected SqlQueryFormatterOptions options;
		protected readonly TypeDescriptorProvider typeDescriptorProvider;

		public IndentationContext AcquireIndentationContext()
		{
			return new IndentationContext(this);
		}
		
		public Sql92QueryFormatter(SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default, SqlDialect sqlDialect = null, SqlDataTypeProvider sqlDataTypeProvider = null, TypeDescriptorProvider typeDescriptorProvider = null)
			: base(sqlDialect, new StringWriter(new StringBuilder()), sqlDataTypeProvider)
		{
			this.options = options;
			this.typeDescriptorProvider = typeDescriptorProvider;
			this.stringQuote = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.StringQuote);
			this.identifierQuoteString = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.IdentifierQuote);
		}

		protected override Expression PreProcess(Expression expression)
		{
			expression = base.PreProcess(expression);
			
			if (this.sqlDialect.SupportsCapability(SqlCapability.AlterTableAddConstraints))
			{
				expression = SqlForeignKeyConstraintToAlterAmender.Amend(expression);
			}

			return expression;
		}

		protected virtual void WriteInsertDefaultValuesSuffix()
		{
			Write(" DEFAULT VALUES");
		}

		protected virtual void WriteInsertIntoReturning(SqlInsertIntoExpression expression)
		{
			if (expression.ReturningAutoIncrementColumnNames == null
				||  expression.ReturningAutoIncrementColumnNames.Count == 0)
			{
				return;
			}

			Write(" RETURNING (");
			WriteDeliminatedListOfItems<string>(expression.ReturningAutoIncrementColumnNames, WriteQuotedIdentifier, ",");
			Write(")");
		}

		public virtual void AppendFullyQualifiedQuotedTableOrTypeName(string tableName, Action<string> append)
		{
			append(this.identifierQuoteString);
			append(tableName);
			append(this.identifierQuoteString);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var retval = Visit(projection.Select);

			return retval;
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method == MethodInfoFastRef.EnumToObjectMethod)
			{
				Visit(methodCallExpression.Arguments[1]);

				return methodCallExpression;
			}
			else if (methodCallExpression.Method == MethodInfoFastRef.ObjectToStringMethod)
			{
				Visit(methodCallExpression.Object);

				return methodCallExpression;
			}
			else if (methodCallExpression.Method.DeclaringType?.GetGenericTypeDefinitionOrNull() == typeof(Nullable<>)
					 && methodCallExpression.Method.Name == "GetValueOrDefault")
			{
				Visit(methodCallExpression.Object);

				return methodCallExpression;
			}
			else if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.DataAccessObjectExtensionsAddToCollectionMethod)
			{
				return Visit(methodCallExpression.Arguments[0]);
			}
			else if (methodCallExpression.Type.IsIntegralType() && methodCallExpression.Method.MemberIsConversionMember())
			{
				return Visit(methodCallExpression.Object ?? methodCallExpression.Arguments[0]);
			}

			throw new NotSupportedException($"The method '{methodCallExpression.Method.Name}' is not supported");
		}

		private static bool IsLikeCallExpression(Expression expression)
		{
			var methodCallExpression = expression as MethodCallExpression;

			if (methodCallExpression == null)
			{
				return false;
			}

			return methodCallExpression.Method.DeclaringType == typeof(ShaolinqStringExtensions)
				   && methodCallExpression.Method.Name == "IsLike";
		}

		private static bool IsNumeric(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return true;
			}

			return false;
		}

		protected override Expression VisitParameter(ParameterExpression expression)
		{
			if (expression.Name.StartsWith("!"))
			{
				Write(expression.Name.Substring(1));
			}
			else
			{
				Write(this.ParameterIndicatorPrefix);
				Write(expression.Name);
			}

			return expression;
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			switch (unaryExpression.NodeType)
			{
			case ExpressionType.Convert:
				var unaryType = unaryExpression.Type.GetUnwrappedNullableType();
				var operandType = unaryExpression.Operand.Type.GetUnwrappedNullableType();

				if (operandType == typeof(object) || unaryType == typeof(object)
					|| unaryType == operandType
					|| (IsNumeric(unaryType) && IsNumeric(operandType))
					|| unaryExpression.Operand.Type.IsDataAccessObjectType())
				{
					Visit(unaryExpression.Operand);
				}
				else if (unaryType.IsIntegralType())
				{
					Visit(unaryExpression.Operand);
				}
				else
				{
					throw new NotSupportedException($"The unary operator '{unaryExpression.NodeType}' is not supported");
				}
			break;
			case ExpressionType.Negate:
			case ExpressionType.NegateChecked:
				Write("(-(");
				Visit(unaryExpression.Operand);
				Write("))");
				break;
			case ExpressionType.Not:
				Write("NOT (");
				Visit(unaryExpression.Operand);
				Write(")");
				break;
			default:
				throw new NotSupportedException($"The unary operator '{unaryExpression.NodeType}' is not supported");
			}

			return unaryExpression;
		}

		protected virtual FunctionResolveResult ResolveSqlFunction(SqlFunctionCallExpression functionExpression)
		{
			var function = functionExpression.Function;
			var arguments = functionExpression.Arguments;

			switch (function)
			{
			case SqlFunction.IsNull:
				return new FunctionResolveResult("", true, arguments)
				{
					functionSuffix = " IS NULL"
				};
			case SqlFunction.IsNotNull:
				return new FunctionResolveResult("", true, arguments)
				{
					functionSuffix = " IS NOT NULL"
				};
			case SqlFunction.In:
				return new FunctionResolveResult("IN", true, arguments);
			case SqlFunction.Exists:
				return new FunctionResolveResult("EXISTSOPERATOR", true, arguments)
				{
					functionPrefix = " EXISTS "
				};
			case SqlFunction.UserDefined:
				return new FunctionResolveResult(functionExpression.UserDefinedFunctionName, false, arguments);
			case SqlFunction.Coalesce:
				return new FunctionResolveResult("COALESCE", false, arguments);
			case SqlFunction.Like:
				return new FunctionResolveResult(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Like), true, arguments);
			case SqlFunction.CompareObject:
				var expressionType = (ExpressionType)((ConstantExpression)arguments[0].StripConstantWrappers()).Value;
				var args = new Expression[2];

				args[0] = arguments[1];
				args[1] = arguments[2];

				switch (expressionType)
				{
					case ExpressionType.LessThan:
						return new FunctionResolveResult("<", true, args.ToReadOnlyCollection());
					case ExpressionType.LessThanOrEqual:
						return new FunctionResolveResult("<=", true, args.ToReadOnlyCollection());
					case ExpressionType.GreaterThan:
						return new FunctionResolveResult(">", true, args.ToReadOnlyCollection());
					case ExpressionType.GreaterThanOrEqual:
						return new FunctionResolveResult(">=", true, args.ToReadOnlyCollection());
				}
				throw new InvalidOperationException();
			case SqlFunction.NotLike:
				return new FunctionResolveResult("NOT " + this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Like), true, arguments);
			case SqlFunction.ServerNow:
				return new FunctionResolveResult("NOW", false, arguments);
			case SqlFunction.ServerUtcNow:
				return new FunctionResolveResult("UTCNOW", false, arguments);
			case SqlFunction.StartsWith:
			{
				var newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, arguments[1], Expression.Constant("%"));
				var optimisedNewArgument = SqlRedundantFunctionCallRemover.Remove(newArgument);

				if (optimisedNewArgument != newArgument)
				{
					if (SqlExpressionFinder.FindExists(arguments[1], c => c.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder))
					{
						this.canReuse = false;
						this.parameterIndexToPlaceholderIndexes = null;
					}
				}

				var list = new List<Expression>
				{
					arguments[0],
					optimisedNewArgument
				};

				return new FunctionResolveResult(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Like), true, list.ToReadOnlyCollection());
			}
			case SqlFunction.ContainsString:
			{
				var newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, arguments[1], Expression.Constant("%"));
				newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, Expression.Constant("%"), newArgument);
				var optimisedNewArgument = SqlRedundantFunctionCallRemover.Remove(newArgument);

				if (optimisedNewArgument != newArgument)
				{
					if (SqlExpressionFinder.FindExists(arguments[1], c => c.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder))
					{
						this.canReuse = false;
						this.parameterIndexToPlaceholderIndexes = null;
					}
				}

				var list = new List<Expression>
				{
					arguments[0],
					optimisedNewArgument
				};

				return new FunctionResolveResult(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Like), true, list.ToReadOnlyCollection());
			}
			case SqlFunction.EndsWith:
			{
				var newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, Expression.Constant("%"), arguments[1]);
				var optimisedNewArgument = SqlRedundantFunctionCallRemover.Remove(newArgument);

				if (optimisedNewArgument != newArgument)
				{
					if (SqlExpressionFinder.FindExists(arguments[1], c => c.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder))
					{
						this.canReuse = false;
						this.parameterIndexToPlaceholderIndexes = null;
					}
				}

				var list = new List<Expression>
				{
					arguments[0],
					optimisedNewArgument
				};

				return new FunctionResolveResult(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Like), true, list.ToReadOnlyCollection());
			}
			case SqlFunction.StringLength:
				return new FunctionResolveResult("LENGTH", false, arguments);
			default:
				return new FunctionResolveResult(function.ToString().ToUpper(), false, arguments);
			}
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			var result = ResolveSqlFunction(functionCallExpression);

			if (result.treatAsOperator)
			{
				Write("(");

				if (result.functionPrefix != null)
				{
					Write(result.functionPrefix);
				}

				for (int i = 0, n = result.arguments.Count - 1; i <= n; i++)
				{
					Visit(result.arguments[i]);

					if (i != n)
					{
						Write(' ');
						Write(result.functionName);
						Write(' ');
					}
				}

				if (result.functionSuffix != null)
				{
					Write(result.functionSuffix);
				}

				Write(")");
			}
			else
			{
				Write(result.functionName);

				if (!result.excludeParenthesis)
				{
					Write("(");
				}

				if (result.functionPrefix != null)
				{
					Write(result.functionPrefix);
				}

				if (result.argsBefore != null && result.argsBefore.Length > 0)
				{
					for (int i = 0, n = result.argsBefore.Length - 1; i <= n; i++)
					{
						Write(this.ParameterIndicatorPrefix);
						Write(ParamNamePrefix);
						Write(this.parameterValues.Count);
						this.parameterValues.Add(new TypedValue(result.argsBefore[i].Type, result.argsBefore[i].Value));

						if (i != n || (functionCallExpression.Arguments.Count > 0))
						{
							Write(", ");
						}
					}
				}

				for (int i = 0, n = result.arguments.Count - 1; i <= n; i++)
				{
					Visit(result.arguments[i]);

					if (i != n || (result.argsAfter != null && result.argsAfter.Length > 0))
					{
						Write(", ");
					}
				}

				if (result.argsAfter != null && result.argsAfter.Length > 0)
				{
					for (int i = 0, n = result.argsAfter.Length - 1; i <= n; i++)
					{
						Write(this.ParameterIndicatorPrefix);
						Write(ParamNamePrefix);
						Write(this.parameterValues.Count);
						this.parameterValues.Add(new TypedValue(result.argsAfter[i].Type, result.argsAfter[i].Value));

						if (i != n)
						{
							Write(", ");
						}
					}
				}

				if (result.functionSuffix != null)
				{
					Write(result.functionSuffix);
				}

				if (!result.excludeParenthesis)
				{
					Write(")");
				}
			}

			return functionCallExpression;
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (binaryExpression.NodeType != ExpressionType.Assign)
			{
				Write("(");
				Write("(");
			}

			Visit(binaryExpression.Left);

			if (binaryExpression.NodeType != ExpressionType.Assign)
			{
				Write(")");
			}

			switch (binaryExpression.NodeType)
			{
			case ExpressionType.And:
			case ExpressionType.AndAlso:
				Write(" AND ");
				break;
			case ExpressionType.Or:
			case ExpressionType.OrElse:
				Write(" OR ");
				break;
			case ExpressionType.Equal:
				Write(" = ");
				break;
			case ExpressionType.NotEqual:
				Write(" <> ");
				break;
			case ExpressionType.LessThan:
				Write(" < ");
				break;
			case ExpressionType.LessThanOrEqual:
				Write(" <= ");
				break;
			case ExpressionType.GreaterThan:
				Write(" > ");
				break;
			case ExpressionType.GreaterThanOrEqual:
				Write(" >= ");
				break;
			case ExpressionType.Add:
				Write(" + ");
				break;
			case ExpressionType.Subtract:
				Write(" - ");
				break;
			case ExpressionType.Multiply:
				Write(" * ");
				break;
			case ExpressionType.Divide:
				Write(" / ");
				break;
			case ExpressionType.Assign:
				Write(" = ");
				break;
			default:
				throw new NotSupportedException($"The binary operator '{binaryExpression.NodeType}' is not supported");
			}

			if (binaryExpression.NodeType != ExpressionType.Assign)
			{
				Write("(");
			}

			Visit(binaryExpression.Right);

			if (binaryExpression.NodeType != ExpressionType.Assign)
			{
				Write(")");
				Write(")");
			}

			return binaryExpression;
		}
			
		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholderExpression)
		{
			if ((this.options & SqlQueryFormatterOptions.EvaluateConstantPlaceholders) != 0)
			{
				var startIndex = this.parameterValues.Count;

				var savedOptions = this.options;

				// Never optimise-out null constant placeholders

				this.options &= ~SqlQueryFormatterOptions.OptimiseOutConstantNulls;

				var retval = base.VisitConstantPlaceholder(constantPlaceholderExpression);

				this.options = savedOptions;

				var endIndex = this.parameterValues.Count;

				if (endIndex - startIndex == 1 && this.canReuse)
				{
					var index = startIndex;

					this.parameterIndexToPlaceholderIndexes.Add(new Pair<int, int>(index, constantPlaceholderExpression.Index));
				}

				return retval;
			}
			else
			{
				WriteFormat("$${0}", constantPlaceholderExpression.Index);

				return constantPlaceholderExpression;
			}
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (constantExpression.Value == null)
			{
				if ((this.options & SqlQueryFormatterOptions.OptimiseOutConstantNulls) != 0 || (this.options & SqlQueryFormatterOptions.EvaluateConstants) != 0)
				{
					Write(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Null));
				}
				else
				{
					Write(this.ParameterIndicatorPrefix);
					Write(ParamNamePrefix);
					Write(this.parameterValues.Count);
					this.parameterValues.Add(new TypedValue(constantExpression.Type, null));
				}
			}
			else
			{
				if (typeof(SqlValuesEnumerable).IsAssignableFrom(constantExpression.Type))
				{
					this.canReuse = false;
					this.parameterIndexToPlaceholderIndexes = null;

					Write("(");
					WriteDeliminatedListOfItems((IEnumerable)constantExpression.Value, c => VisitConstant(Expression.Constant(c)));
					Write(")");
				}
				else
				{
					if ((this.options & SqlQueryFormatterOptions.EvaluateConstants) != 0)
					{
						Write(FormatConstant(constantExpression.Value));
					}
					else
					{
						Write(this.ParameterIndicatorPrefix);
						Write(ParamNamePrefix);
						Write(this.parameterValues.Count);
						this.parameterValues.Add(new TypedValue(constantExpression.Type, constantExpression.Value));
					}
				}
			}

			return constantExpression;
		}

		private static string GetAggregateName(SqlAggregateType aggregateType)
		{
			switch (aggregateType)
			{
				case SqlAggregateType.Count:
					return "COUNT";
				case SqlAggregateType.LongCount:
					return "COUNT";
				case SqlAggregateType.Min:
					return "MIN";
				case SqlAggregateType.Max:
					return "MAX";
				case SqlAggregateType.Sum:
					return "SUM";
				case SqlAggregateType.Average:
					return "AVG";
				default:
					throw new NotSupportedException($"Unknown aggregate type: {aggregateType}");
			}
		}

		protected virtual bool RequiresAsteriskWhenNoArgument(SqlAggregateType aggregateType)
		{
			return aggregateType == SqlAggregateType.Count || aggregateType == SqlAggregateType.LongCount;
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			Write(GetAggregateName(sqlAggregate.AggregateType));

			Write("(");

			if (sqlAggregate.IsDistinct)
			{
				Write("DISTINCT ");
			}

			if (sqlAggregate.Argument != null)
			{
				Visit(sqlAggregate.Argument);
			}
			else if (RequiresAsteriskWhenNoArgument(sqlAggregate.AggregateType))
			{
				Write("*");
			}

			Write(")");

			return sqlAggregate;
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			Write("(");

			using (AcquireIndentationContext())
			{
				Visit(subquery.Select);
				WriteLine();
			}

			Write(")");

			return subquery;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (!String.IsNullOrEmpty(columnExpression.SelectAlias))
			{
				if (this.ignoreAlias == columnExpression.SelectAlias)
				{
					WriteQuotedIdentifier(this.replaceAlias);
				}
				else
				{
					WriteQuotedIdentifier(columnExpression.SelectAlias);
				}

				Write(".");
			}

			WriteQuotedIdentifier(columnExpression.Name);
			
			return columnExpression;
		}

		protected override Expression VisitVariableDeclaration(SqlVariableDeclarationExpression expression)
		{
			Write(this.ParameterIndicatorPrefix);
			Write(expression.Name);
			Write(" ");
			WriteTypeName(this.sqlDataTypeProvider.GetSqlDataType(expression.Type).GetSqlName(null));
			
			return base.VisitVariableDeclaration(expression);
		}

		protected virtual void VisitColumn(SqlSelectExpression selectExpression, SqlColumnDeclaration column)
		{
			if ((!(Visit(column.Expression) is SqlColumnExpression c) || c.Name != column.Name) && !String.IsNullOrEmpty(column.Name))
			{
				Write(" AS ");
				WriteQuotedIdentifier(column.Name);
			}
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			Write("CASE WHEN (");
			Visit(expression.Test);
			Write(")");
			Write(" THEN (");
			Visit(expression.IfTrue);
			Write(") ELSE (");
			Visit(expression.IfFalse);
			Write(") END");

			return expression;
		}

		private int selectNest;

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var selectNested = this.selectNest > 0;

			if (selectNested)
			{
				Write("(");
			}

			try
			{
				this.selectNest++;

				if (selectExpression.From?.NodeType == (ExpressionType)SqlExpressionType.Delete)
				{
					Visit(selectExpression.From);

					return selectExpression;
				}

				Write("SELECT ");

				if (selectExpression.Distinct)
				{
					Write("DISTINCT ");
				}

				AppendTop(selectExpression);

				if ((selectExpression.Columns?.Count ?? 0) == 0)
				{
					Write("* ");
				}
				else
				{
					for (int i = 0, n = selectExpression.Columns.Count; i < n; i++)
					{
						var column = selectExpression.Columns[i];

						if (i > 0)
						{
							Write(", ");
						}

						VisitColumn(selectExpression, column);
					}
				}

				if (selectExpression.Into != null)
				{
					WriteLine();
					Write("INTO ");
					VisitSource(selectExpression.Into);
				}

				if (selectExpression.From != null)
				{
					WriteLine();
					Write("FROM ");
					VisitSource(selectExpression.From);
				}

				if (selectExpression.Where != null)
				{
					WriteLine();
					Write("WHERE ");
					Visit(selectExpression.Where);
				}


				if (selectExpression.GroupBy != null && selectExpression.GroupBy.Count > 0)
				{
					WriteLine();
					Write("GROUP BY ");

					WriteDeliminatedListOfItems(selectExpression.GroupBy, c => Visit(c));
				}

				if (selectExpression.OrderBy != null && selectExpression.OrderBy.Count > 0)
				{
					WriteLine();
					Write("ORDER BY ");

					WriteDeliminatedListOfItems<Expression>(selectExpression.OrderBy, c =>
					{
						Visit(c);
					});
				}

				AppendLimit(selectExpression);

				if (selectExpression.ForUpdate && this.sqlDialect.SupportsCapability(SqlCapability.SelectForUpdate))
				{
					Write(" FOR UPDATE");
				}

				if (selectNested)
				{
					Write(")");
				}
			}
			finally
			{
				this.selectNest--;
			}

			return selectExpression;
		}

		protected override Expression VisitOrderBy(SqlOrderByExpression orderByExpression)
		{
			base.VisitOrderBy(orderByExpression);

			if (orderByExpression.OrderType == OrderType.Descending)
			{
				Write(" DESC");
			}

			return orderByExpression;
		}

		protected virtual void AppendTop(SqlSelectExpression selectExpression)
		{
		}

		protected virtual void AppendLimit(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null || selectExpression.Take != null)
			{
				Write(" LIMIT ");

				if (selectExpression.Skip == null)
				{
					Write("0");
				}
				else
				{
					Visit(selectExpression.Skip);
				}

				if (selectExpression.Take != null)
				{
					Write(", ");

					Visit(selectExpression.Take);
				}
				else if (selectExpression.Skip != null)
				{
					Write(", ");
					Write(Int64.MaxValue);
				}
			}
		}

		protected virtual void Write(SqlJoinType joinType)
		{
			switch (joinType)
			{
			case SqlJoinType.Cross:
				Write(" CROSS JOIN ");
				break;
			case SqlJoinType.Inner:
				Write(" INNER JOIN ");
				break;
			case SqlJoinType.Left:
				Write(" LEFT JOIN ");
				break;
			case SqlJoinType.Right:
				Write(" RIGHT JOIN ");
				break;
			case SqlJoinType.Outer:
				Write(" FULL OUTER JOIN ");
				break;
			case SqlJoinType.CrossApply:
				Write(" CROSS APPLY ");
				break;
			case SqlJoinType.OuterApply:
				Write(" OUTER APPLY ");
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(joinType), joinType, $"JoinType {joinType} not supported");
			}
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			VisitSource(join.Left);

			WriteLine();

			Write(join.JoinType);

			VisitSource(join.Right);

			if (join.JoinCondition != null)
			{
				using (AcquireIndentationContext())
				{
					Write("ON ");

					Visit(join.JoinCondition);
				}
			}

			return join;
		}

		protected override Expression VisitTable(SqlTableExpression expression)
		{
			WriteTableName(expression.Name);

			return expression;
		}

		protected override Expression VisitSource(Expression source)
		{
			switch ((SqlExpressionType)source.NodeType)
			{
			case SqlExpressionType.Table:
				var table = (SqlTableExpression)source;

				Visit(table);

				if (table.Alias != null)
				{
					Write(" AS ");
					WriteQuotedIdentifier(table.Alias);
				}

				break;
			case SqlExpressionType.Select:
				var select = (SqlSelectExpression)source;
				WriteLine();
				Write("(");

				using (AcquireIndentationContext())
				{
					Visit(select);
					WriteLine();
				}

				Write(")");
				Write(" AS ");
				WriteQuotedIdentifier(select.Alias);

				break;
			case SqlExpressionType.Join:
				VisitJoin((SqlJoinExpression)source);
				break;
			case SqlExpressionType.Delete:
				VisitDelete((SqlDeleteExpression)source);
				break;
			case SqlExpressionType.Update:
				VisitUpdate((SqlUpdateExpression)source);
				break;
			case SqlExpressionType.Union:
				VisitUnion((SqlUnionExpression)source);
				break;
			default:
				throw new InvalidOperationException($"Select source ({source.NodeType}) is not valid type");
			}

			return source;
		}

		protected string ignoreAlias;
		protected string replaceAlias;
		protected readonly string identifierQuoteString;
		private readonly string stringQuote;

		protected virtual void WriteTableName(string tableName)
		{
			AppendFullyQualifiedQuotedTableOrTypeName(tableName, Write);
		}

		protected virtual void WriteTypeName(string typeName)
		{
			AppendFullyQualifiedQuotedTableOrTypeName(typeName, Write);
		}

		protected override Expression VisitDelete(SqlDeleteExpression deleteExpression)
		{
			Write("DELETE ");
			Write("FROM ");
			Visit(deleteExpression.Source);

			if (deleteExpression.Where != null)
			{
				WriteLine();
				Write(" WHERE ");
				WriteLine();
				Visit(deleteExpression.Where);
			}

			WriteLine(";");

			return deleteExpression;
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			var declaringType = memberExpression.Member.DeclaringType;

			if (declaringType != null && Nullable.GetUnderlyingType(declaringType) != null)
			{
				return Visit(memberExpression.Expression);
			}
			else if (memberExpression.Type.IsIntegralType() && memberExpression.Member.MemberIsConversionMember())
			{
				return Visit(memberExpression.Expression);
			}

			Visit(memberExpression.Expression);
			Write(".");
			Write("Prop(");
			Write(memberExpression.Member.Name);
			Write(")");

			return memberExpression;
		}

		protected override Expression VisitObjectReference(SqlObjectReferenceExpression objectReferenceExpression)
		{
			Write("ObjectReference(");
			Write(objectReferenceExpression.Type.Name);
			Write(")");

			return objectReferenceExpression;
		}

		protected override Expression VisitTuple(SqlTupleExpression tupleExpression)
		{
			Write('(');
			WriteDeliminatedListOfItems(tupleExpression.SubExpressions, c => Visit(c));
			Write(')');

			return tupleExpression;
		}

		protected override Expression VisitCreateIndex(SqlCreateIndexExpression createIndexExpression)
		{
			Write("CREATE ");

			if (createIndexExpression.Unique)
			{
				Write("UNIQUE ");
			}

			if (createIndexExpression.Clustered != null)
			{
				if (createIndexExpression.Clustered.Value)
				{
					Write("CLUSTERED ");
				}
				else
				{
					Write("NONCLUSTERED ");
				}
			}

			if (createIndexExpression.IfNotExist)
			{
				Write("IF NOT EXIST ");
			}

			Write("INDEX ");
			WriteQuotedIdentifier(createIndexExpression.IndexName);
			Write(" ON ");
			Visit(createIndexExpression.Table);
			Write("(");
			WriteDeliminatedListOfItems(createIndexExpression.Columns, c => Visit(c));
			Write(")");

			if (this.sqlDialect.SupportsCapability(SqlCapability.IndexInclude))
			{
				if (createIndexExpression.IncludedColumns?.Count > 0)
				{
					Write(" INCLUDE ");

					Write("(");
					WriteDeliminatedListOfItems(createIndexExpression.IncludedColumns, c => Visit(c));
					WriteLine(")");
				}
			}

			if (createIndexExpression.Where != null)
			{
				Write(" WHERE ");
				Visit(createIndexExpression.Where);
			}

			WriteLine(";");

			return createIndexExpression;
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			Write("CREATE TABLE ");
			Visit(createTableExpression.Table);
			WriteLine();
			Write("(");

			using (AcquireIndentationContext())
			{
				WriteDeliminatedListOfItems(createTableExpression.ColumnDefinitionExpressions, c => Visit(c), () => WriteLine(","));

				if (createTableExpression.ColumnDefinitionExpressions.Count > 0 && createTableExpression.TableConstraints.Count > 0)
				{
					Write(",");
				}

				WriteLine();
				WriteDeliminatedListOfItems(createTableExpression.TableConstraints, c => Visit(c), () => WriteLine(","));
			}

			WriteLine();
			WriteLine(");");

			return createTableExpression;
		}

		protected virtual void Write(SqlColumnReferenceAction action)
		{
			switch (action)
			{
			case SqlColumnReferenceAction.Cascade:
				Write("CASCADE");
				break;
			case SqlColumnReferenceAction.Restrict:
				Write("RESTRICT");
				break;
			case SqlColumnReferenceAction.SetDefault:
				Write("SET DEFAULT");
				break;
			case SqlColumnReferenceAction.SetNull:
				Write("SET NULL");
				break;
			}
		}

		protected override Expression VisitConstraint(SqlConstraintExpression expression)
		{
			if (!expression.ConstraintName.IsNullOrEmpty())
			{
				Write("CONSTRAINT ");
				WriteQuotedIdentifier(expression.ConstraintName);
				Write(" ");
			}

			if (expression.PrimaryKey)
			{
				Write("PRIMARY KEY");

				if (expression.KeyOptions != null)
				{
					Write(" ");
					WriteDeliminatedListOfItems(expression.KeyOptions, Write, " ");
					Write(" ");
				}

				if (expression.ColumnNames != null)
				{
					Write("(");
					WriteDeliminatedListOfItems(expression.ColumnNames, WriteQuotedIdentifier);
					Write(")");
				}
			}
			else if (expression.Unique)
			{
				Write("UNIQUE");

				if (expression.KeyOptions != null)
				{
					Write(" ");
					WriteDeliminatedListOfItems(expression.KeyOptions, Write, " ");
					Write(" ");
				}

				if (expression.ColumnNames != null)
				{
					Write("(");
					WriteDeliminatedListOfItems(expression.ColumnNames, WriteQuotedIdentifier);
					Write(")");
				}
			}
			
			if (expression.NotNull)
			{
				Write("NOT NULL");
			}

			if (expression.AutoIncrement)
			{
				var s = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.AutoIncrement);

				if (!string.IsNullOrEmpty(s))
				{
					Write(s);
				}
			}

			if (expression.ReferencesExpression != null)
			{
				if (expression.ColumnNames != null)
				{
					Write("FOREIGN KEY ");

					Write("(");
					WriteDeliminatedListOfItems(expression.ColumnNames, WriteQuotedIdentifier);
					Write(") ");
				}

				Visit(expression.ReferencesExpression);
			}

			if (expression.DefaultValue != null)
			{
				Write("DEFAULT");
				Write(" ");
				Visit(expression.DefaultValue);
			}

			return expression;
		}

		protected virtual void WriteQuotedIdentifier(string identifierName)
		{
			Write(this.identifierQuoteString);
			Write(identifierName);
			Write(this.identifierQuoteString);
		}

		protected virtual void WriteQuotedString(string value)
		{
			Write(this.stringQuote);
			Write(value);
			Write(this.stringQuote);
		}
		
		public virtual void WriteQuotedStringOrObject(object value)
		{

			if (value is string s)
			{
				WriteQuotedString(s);
			}
			else
			{
				this.writer.Write(value);
			}
		}

		protected override Expression VisitReferences(SqlReferencesExpression referencesExpression)
		{
			Write("REFERENCES ");
			Visit(referencesExpression.ReferencedTable);
			Write("(");

			WriteDeliminatedListOfItems(referencesExpression.ReferencedColumnNames, WriteQuotedIdentifier);

			Write(")");

			if (referencesExpression.OnDeleteAction != SqlColumnReferenceAction.NoAction)
			{
				Write(" ON DELETE ");
				Write(referencesExpression.OnDeleteAction);
			}

			if (referencesExpression.OnUpdateAction != SqlColumnReferenceAction.NoAction)
			{
				Write(" ON UPDATE ");

				Write(referencesExpression.OnUpdateAction);
			}

			if (this.sqlDialect.SupportsCapability(SqlCapability.Deferrability))
			{
				WriteDeferrability(referencesExpression.Deferrability);
			}

			return referencesExpression;
		}

		protected virtual void WriteDeferrability(SqlColumnReferenceDeferrability deferrability)
		{
			switch (deferrability)
			{
			case SqlColumnReferenceDeferrability.Deferrable:
				Write(" DEFERRABLE");
				break;
			case SqlColumnReferenceDeferrability.InitiallyDeferred:
				Write(" DEFERRABLE INITIALLY DEFERRED");
				break;
			case SqlColumnReferenceDeferrability.InitiallyImmediate:
				Write(" DEFERRABLE INITIALLY IMMEDIATE");
				break;
			}
		}

		protected override Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			WriteQuotedIdentifier(columnDefinitionExpression.ColumnName);
			Write(' ');
			Visit(columnDefinitionExpression.ColumnType);

			if (columnDefinitionExpression.ConstraintExpressions.Count > 0)
			{
				Write(' ');
			}

			WriteDeliminatedListOfItems(columnDefinitionExpression.ConstraintExpressions, c => Visit(c), " ");

			return columnDefinitionExpression;
		}

		protected override Expression VisitConstraintAction(SqlConstraintActionExpression actionExpression)
		{
			Write(actionExpression.ActionType.ToString().ToUpper());
			Write(" ");
			Visit(actionExpression.ConstraintExpression);

			return actionExpression;
		}

		protected override Expression VisitAlterTable(SqlAlterTableExpression alterTableExpression)
		{
			Write("ALTER TABLE ");
			Visit(alterTableExpression.Table);
			Write(" ");

			if (alterTableExpression.ConstraintActions != null)
			{
				VisitExpressionList(alterTableExpression.ConstraintActions);
			}
			else if (alterTableExpression.Actions != null)
			{
				foreach (var action in alterTableExpression.Actions)
				{
					Visit(action);
				}
			}

			WriteLine(";");

			return alterTableExpression;
		}

		protected virtual bool WriteInsertIntoAfterSource(SqlInsertIntoExpression expression)
		{
			return false;
		}

		protected override Expression VisitInsertInto(SqlInsertIntoExpression expression)
		{
			Write("INSERT INTO ");
			Visit(expression.Source);

			if ((expression.ValueExpressions == null || expression.ValueExpressions.Count == 0) && expression.ValuesExpression == null)
			{
				if (this.sqlDialect.SupportsCapability(SqlCapability.InsertOutput))
				{
					WriteInsertIntoReturning(expression);
					Write(" ");
				}

				WriteInsertDefaultValuesSuffix();
			}
			else
			{
				WriteInsertIntoAfterSource(expression);

				Write("(");
				WriteDeliminatedListOfItems(expression.ColumnNames, WriteQuotedIdentifier);

				Write(") ");

				if (this.sqlDialect.SupportsCapability(SqlCapability.InsertOutput))
				{
					WriteInsertIntoReturning(expression);
					Write(" ");
				}
				
				if (expression.ValuesExpression != null)
				{
					Visit(expression.ValuesExpression);
				}
				else
				{
					Write("VALUES (");
					WriteDeliminatedListOfItems (expression.ValueExpressions, c =>
					{
						Write("(");
						Visit(c);
						Write(")");
					});
					Write(")");
				}
			}

			if (!this.sqlDialect.SupportsCapability(SqlCapability.InsertOutput))
			{
				WriteInsertIntoReturning(expression);
			}

			Write(";");

			return expression;
		}

		protected override Expression VisitAssign(SqlAssignExpression expression)
		{
			Visit(expression.Target);
			Write(" = ");
			Write("(");
			Visit(expression.Value);
			Write(")");

			return expression;
		}

		protected override Expression VisitUpdate(SqlUpdateExpression expression)
		{
			Write("UPDATE ");
			Visit(expression.Source);
			Write(" SET ");

			WriteDeliminatedListOfItems(expression.Assignments, c => Visit(c));

			if (expression.Where == null)
			{
				Write(";");
			}

			Write(" WHERE ");
			Visit(expression.Where);
			Write(";");

			return expression;
		}

		protected override Expression VisitCreateType(SqlCreateTypeExpression expression)
		{
			Write("CREATE TYPE ");
			Visit(expression.SqlType);
			Write(" AS ");

			Visit(expression.AsExpression);

			WriteLine(";");

			return expression;
		}

		protected override Expression VisitEnumDefinition(SqlEnumDefinitionExpression expression)
		{
			Write("ENUM (");
			WriteDeliminatedListOfItems(expression.Labels, WriteQuotedString);
			Write(")");

			return expression;
		}

		protected override Expression VisitType(SqlTypeExpression expression)
		{
			if (expression.UserDefinedType)
			{
				WriteQuotedIdentifier(expression.TypeName);
			}
			else
			{
				Write(expression.TypeName);
			}

			return expression;
		}

		protected override Expression VisitStatementList(SqlStatementListExpression statementListExpression)
		{
			var i = 0;

			foreach (var statement in statementListExpression.Statements)
			{
				Visit(statement);

				if (i != statementListExpression.Statements.Count - 1)
				{
					if (statement is SqlSelectExpression)
					{
						WriteLine(";");
					}
					else
					{
						WriteLine();
					}
				}
			}

			return statementListExpression;
		}

		protected override Expression VisitIndexedColumn(SqlIndexedColumnExpression indexedColumnExpression)
		{
			Visit(indexedColumnExpression.Column);

			switch (indexedColumnExpression.SortOrder)
			{
			case SortOrder.Descending:
				Write(" DESC");
				break;
			case SortOrder.Ascending:
				Write(" ASC");
				break;
			case SortOrder.Unspecified:
				break;
			}

			return indexedColumnExpression;
		}

		protected override Expression VisitPragma(SqlPragmaExpression expression)
		{
			Write("PRAGMA ");
			Write(expression.Directive);
			WriteLine(";");

			return expression;
		}

		protected override Expression VisitKeyword(SqlKeywordExpression expression)
		{
			Write(' ');
			Write(expression.Name);
			Write(' ');

			return expression;
		}

		protected override Expression VisitSetCommand(SqlSetCommandExpression expression)
		{
			Write("SET ");

			if (expression.ConfigurationParameter != null)
			{
				Write(expression.ConfigurationParameter);
			}

			if (expression.Target != null)
			{
				Write(" ");
				Visit(expression.Target);
				Write(" ");
			}

			Write(" ");
			WriteDeliminatedListOfItems(expression.Arguments, c => Visit(c));
			WriteLine(";");
			
			return expression;
		}

		protected override Expression VisitUnion(SqlUnionExpression expression)
		{
			var savedSelectNext = this.selectNest;
			this.selectNest = 0;

			Write("(");
			Visit(expression.Left);
			if (expression.UnionAll)
			{
				Write(" UNION ALL ");
			}
			else
			{
				Write(" UNION ");
			}
			Visit(expression.Right);
			Write(")");
			Write(" AS ");
			WriteQuotedIdentifier(expression.Alias);

			this.selectNest = savedSelectNext;

			return expression;
		}
	}
}