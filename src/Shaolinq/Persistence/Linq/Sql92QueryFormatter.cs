// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
		protected internal static readonly string ParamNamePrefix = "shaolinqparam";
		
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
		protected readonly SqlDataTypeProvider sqlDataTypeProvider;
		protected readonly TypeDescriptorProvider typeDescriptorProvider;

		public IndentationContext AcquireIndentationContext()
		{
			return new IndentationContext(this);
		}
		
		public Sql92QueryFormatter(SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default, SqlDialect sqlDialect = null, SqlDataTypeProvider sqlDataTypeProvider = null, TypeDescriptorProvider typeDescriptorProvider = null)
			: base(sqlDialect, new StringWriter(new StringBuilder()))
		{
			this.options = options;
			this.typeDescriptorProvider = typeDescriptorProvider;
			this.sqlDataTypeProvider = sqlDataTypeProvider ?? new DefaultSqlDataTypeProvider(new ConstraintDefaultsConfiguration());
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
			this.Write(" DEFAULT VALUES");
		}

		protected virtual void WriteInsertIntoReturning(SqlInsertIntoExpression expression)
		{
			if (expression.ReturningAutoIncrementColumnNames == null
				||  expression.ReturningAutoIncrementColumnNames.Count == 0)
			{
				return;
			}

			this.Write(" RETURNING (");
			this.WriteDeliminatedListOfItems<string>(expression.ReturningAutoIncrementColumnNames, this.WriteQuotedIdentifier, ",");
			this.Write(")");
		}

		public virtual void AppendFullyQualifiedQuotedTableOrTypeName(string tableName, Action<string> append)
		{
			append(this.identifierQuoteString);
			append(tableName);
			append(this.identifierQuoteString);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var retval = this.Visit(projection.Select);

			return retval;
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method == MethodInfoFastRef.EnumToObjectMethod)
			{
				this.Visit(methodCallExpression.Arguments[1]);

				return methodCallExpression;
			}
			else if (methodCallExpression.Method == MethodInfoFastRef.ObjectToStringMethod)
			{
				this.Visit(methodCallExpression.Object);

				return methodCallExpression;
			}
			else if (methodCallExpression.Method.DeclaringType?.GetGenericTypeDefinitionOrNull() == typeof(Nullable<>)
					 && methodCallExpression.Method.Name == "GetValueOrDefault")
			{
				this.Visit(methodCallExpression.Object);

				return methodCallExpression;
			}
			else if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.DataAccessObjectExtensionsAddToCollectionMethod)
			{
				return this.Visit(methodCallExpression.Arguments[0]);
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
				this.Write(expression.Name.Substring(1));
			}
			else
			{
				this.Write(this.ParameterIndicatorPrefix);
				this.Write(expression.Name);
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
					this.Visit(unaryExpression.Operand);
				}
				else
				{
					throw new NotSupportedException($"The unary operator '{unaryExpression.NodeType}' is not supported");
				}
				break;
			case ExpressionType.Negate:
			case ExpressionType.NegateChecked:
				this.Write("(-(");
				this.Visit(unaryExpression.Operand);
				this.Write("))");
				break;
			case ExpressionType.Not:
				this.Write("NOT (");
				this.Visit(unaryExpression.Operand);
				this.Write(")");
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
						canReuse = false;
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
						canReuse = false;
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
						canReuse = false;
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
			var result = this.ResolveSqlFunction(functionCallExpression);

			if (result.treatAsOperator)
			{
				this.Write("(");

				if (result.functionPrefix != null)
				{
					this.Write(result.functionPrefix);
				}

				for (int i = 0, n = result.arguments.Count - 1; i <= n; i++)
				{
					this.Visit(result.arguments[i]);

					if (i != n)
					{
						this.Write(' ');
						this.Write(result.functionName);
						this.Write(' ');
					}
				}

				if (result.functionSuffix != null)
				{
					this.Write(result.functionSuffix);
				}

				this.Write(")");
			}
			else
			{
				this.Write(result.functionName);

				if (!result.excludeParenthesis)
				{
					this.Write("(");
				}

				if (result.functionPrefix != null)
				{
					this.Write(result.functionPrefix);
				}

				if (result.argsBefore != null && result.argsBefore.Length > 0)
				{
					for (int i = 0, n = result.argsBefore.Length - 1; i <= n; i++)
					{
						this.Write(this.ParameterIndicatorPrefix);
						this.Write(ParamNamePrefix);
						this.Write(this.parameterValues.Count);
						this.parameterValues.Add(new TypedValue(result.argsBefore[i].Type, result.argsBefore[i].Value));

						if (i != n || (functionCallExpression.Arguments.Count > 0))
						{
							this.Write(", ");
						}
					}
				}

				for (int i = 0, n = result.arguments.Count - 1; i <= n; i++)
				{
					this.Visit(result.arguments[i]);

					if (i != n || (result.argsAfter != null && result.argsAfter.Length > 0))
					{
						this.Write(", ");
					}
				}

				if (result.argsAfter != null && result.argsAfter.Length > 0)
				{
					for (int i = 0, n = result.argsAfter.Length - 1; i <= n; i++)
					{
						this.Write(this.ParameterIndicatorPrefix);
						this.Write(ParamNamePrefix);
						this.Write(this.parameterValues.Count);
						this.parameterValues.Add(new TypedValue(result.argsAfter[i].Type, result.argsAfter[i].Value));

						if (i != n)
						{
							this.Write(", ");
						}
					}
				}

				if (result.functionSuffix != null)
				{
					this.Write(result.functionSuffix);
				}

				if (!result.excludeParenthesis)
				{
					this.Write(")");
				}
			}

			return functionCallExpression;
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (binaryExpression.NodeType != ExpressionType.Assign)
			{
				this.Write("(");
				this.Write("(");
			}

			this.Visit(binaryExpression.Left);

			if (binaryExpression.NodeType != ExpressionType.Assign)
			{
				this.Write(")");
			}

			switch (binaryExpression.NodeType)
			{
			case ExpressionType.And:
			case ExpressionType.AndAlso:
				this.Write(" AND ");
				break;
			case ExpressionType.Or:
			case ExpressionType.OrElse:
				this.Write(" OR ");
				break;
			case ExpressionType.Equal:
				this.Write(" = ");
				break;
			case ExpressionType.NotEqual:
				this.Write(" <> ");
				break;
			case ExpressionType.LessThan:
				this.Write(" < ");
				break;
			case ExpressionType.LessThanOrEqual:
				this.Write(" <= ");
				break;
			case ExpressionType.GreaterThan:
				this.Write(" > ");
				break;
			case ExpressionType.GreaterThanOrEqual:
				this.Write(" >= ");
				break;
			case ExpressionType.Add:
				this.Write(" + ");
				break;
			case ExpressionType.Subtract:
				this.Write(" - ");
				break;
			case ExpressionType.Multiply:
				this.Write(" * ");
				break;
			case ExpressionType.Divide:
				this.Write(" / ");
				break;
			case ExpressionType.Assign:
				this.Write(" = ");
				break;
			default:
				throw new NotSupportedException($"The binary operator '{binaryExpression.NodeType}' is not supported");
			}

			if (binaryExpression.NodeType != ExpressionType.Assign)
			{
				this.Write("(");
			}

			this.Visit(binaryExpression.Right);

			if (binaryExpression.NodeType != ExpressionType.Assign)
			{
				this.Write(")");
				this.Write(")");
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

				if (endIndex - startIndex == 1 && canReuse)
				{
					var index = startIndex;

					parameterIndexToPlaceholderIndexes.Add(new Pair<int, int>(index, constantPlaceholderExpression.Index));
				}

				return retval;
			}
			else
			{
				this.WriteFormat("$${0}", constantPlaceholderExpression.Index);

				return constantPlaceholderExpression;
			}
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (constantExpression.Value == null)
			{
				if ((this.options & SqlQueryFormatterOptions.OptimiseOutConstantNulls) != 0)
				{
					this.Write(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Null));
				}
				else
				{
					this.Write(this.ParameterIndicatorPrefix);
					this.Write(ParamNamePrefix);
					this.Write(this.parameterValues.Count);
					this.parameterValues.Add(new TypedValue(constantExpression.Type, null));
				}
			}
			else
			{
				if (typeof(SqlValuesEnumerable).IsAssignableFrom(constantExpression.Type))
				{
					canReuse = false;
					parameterIndexToPlaceholderIndexes = null;

					this.Write("(");
					this.WriteDeliminatedListOfItems((IEnumerable)constantExpression.Value, c => this.VisitConstant(Expression.Constant(c)));
					this.Write(")");
				}
				else
				{
					this.Write(this.ParameterIndicatorPrefix);
					this.Write(ParamNamePrefix);
					this.Write(this.parameterValues.Count);
					this.parameterValues.Add(new TypedValue(constantExpression.Type, constantExpression.Value));
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
			this.Write(GetAggregateName(sqlAggregate.AggregateType));

			this.Write("(");

			if (sqlAggregate.IsDistinct)
			{
				this.Write("DISTINCT ");
			}

			if (sqlAggregate.Argument != null)
			{
				this.Visit(sqlAggregate.Argument);
			}
			else if (this.RequiresAsteriskWhenNoArgument(sqlAggregate.AggregateType))
			{
				this.Write("*");
			}

			this.Write(")");

			return sqlAggregate;
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			this.Write("(");

			using (this.AcquireIndentationContext())
			{
				this.Visit(subquery.Select);
				this.WriteLine();
			}

			this.Write(")");

			return subquery;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (!String.IsNullOrEmpty(columnExpression.SelectAlias))
			{
				if (this.ignoreAlias == columnExpression.SelectAlias)
				{
					this.WriteQuotedIdentifier(this.replaceAlias);
				}
				else
				{
					this.WriteQuotedIdentifier(columnExpression.SelectAlias);
				}

				this.Write(".");
			}

			this.WriteQuotedIdentifier(columnExpression.Name);
			
			return columnExpression;
		}

		protected override Expression VisitVariableDeclaration(SqlVariableDeclarationExpression expression)
		{
			this.Write(this.ParameterIndicatorPrefix);
			this.Write(expression.Name);
			this.Write(" ");
			this.WriteTypeName(this.sqlDataTypeProvider.GetSqlDataType(expression.Type).GetSqlName(null));
			
			return base.VisitVariableDeclaration(expression);
		}

		protected virtual void VisitColumn(SqlSelectExpression selectExpression, SqlColumnDeclaration column)
		{
			var c = this.Visit(column.Expression) as SqlColumnExpression;

			if ((c == null || c.Name != column.Name) && !String.IsNullOrEmpty(column.Name))
			{
				this.Write(" AS ");
				this.WriteQuotedIdentifier(column.Name);
			}
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			this.Write("CASE WHEN (");
			this.Visit(expression.Test);
			this.Write(")");
			this.Write(" THEN (");
			this.Visit(expression.IfTrue);
			this.Write(") ELSE (");
			this.Visit(expression.IfFalse);
			this.Write(") END");

			return expression;
		}

		private int selectNest;

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var selectNested = this.selectNest > 0;

			if (selectNested)
			{
				this.Write("(");
			}

			try
			{
				this.selectNest++;

				if (selectExpression.From?.NodeType == (ExpressionType)SqlExpressionType.Delete)
				{
					this.Visit(selectExpression.From);

					return selectExpression;
				}

				this.Write("SELECT ");

				if (selectExpression.Distinct)
				{
					this.Write("DISTINCT ");
				}

				this.AppendTop(selectExpression);

				if ((selectExpression.Columns?.Count ?? 0) == 0)
				{
					this.Write("* ");
				}
				else
				{
					for (int i = 0, n = selectExpression.Columns.Count; i < n; i++)
					{
						var column = selectExpression.Columns[i];

						if (i > 0)
						{
							this.Write(", ");
						}

						this.VisitColumn(selectExpression, column);
					}
				}

				if (selectExpression.Into != null)
				{
					this.WriteLine();
					this.Write("INTO ");
					this.VisitSource(selectExpression.Into);
				}

				if (selectExpression.From != null)
				{
					this.WriteLine();
					this.Write("FROM ");
					this.VisitSource(selectExpression.From);
				}

				if (selectExpression.Where != null)
				{
					this.WriteLine();
					this.Write("WHERE ");
					this.Visit(selectExpression.Where);
				}


				if (selectExpression.GroupBy != null && selectExpression.GroupBy.Count > 0)
				{
					this.WriteLine();
					this.Write("GROUP BY ");

					this.WriteDeliminatedListOfItems(selectExpression.GroupBy, c => this.Visit(c));
				}

				if (selectExpression.OrderBy != null && selectExpression.OrderBy.Count > 0)
				{
					this.WriteLine();
					this.Write("ORDER BY ");

					this.WriteDeliminatedListOfItems<Expression>(selectExpression.OrderBy, c =>
					{
						this.Visit(c);
					});
				}

				this.AppendLimit(selectExpression);

				if (selectExpression.ForUpdate && this.sqlDialect.SupportsCapability(SqlCapability.SelectForUpdate))
				{
					this.Write(" FOR UPDATE");
				}

				if (selectNested)
				{
					this.Write(")");
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

			if (((SqlOrderByExpression)orderByExpression).OrderType == OrderType.Descending)
			{
				this.Write(" DESC");
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
				this.Write(" LIMIT ");

				if (selectExpression.Skip == null)
				{
					this.Write("0");
				}
				else
				{
					this.Visit(selectExpression.Skip);
				}

				if (selectExpression.Take != null)
				{
					this.Write(", ");

					this.Visit(selectExpression.Take);
				}
				else if (selectExpression.Skip != null)
				{
					this.Write(", ");
					this.Write(Int64.MaxValue);
				}
			}
		}

		protected virtual void Write(SqlJoinType joinType)
		{
			switch (joinType)
			{
			case SqlJoinType.Cross:
				this.Write(" CROSS JOIN ");
				break;
			case SqlJoinType.Inner:
				this.Write(" INNER JOIN ");
				break;
			case SqlJoinType.Left:
				this.Write(" LEFT JOIN ");
				break;
			case SqlJoinType.Right:
				this.Write(" RIGHT JOIN ");
				break;
			case SqlJoinType.Outer:
				this.Write(" FULL OUTER JOIN ");
				break;
			case SqlJoinType.CrossApply:
				this.Write(" CROSS APPLY ");
				break;
			case SqlJoinType.OuterApply:
				this.Write(" OUTER APPLY ");
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(joinType), joinType, $"JoinType {joinType} not supported");
			}
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			this.VisitSource(join.Left);

			this.WriteLine();

			this.Write(join.JoinType);

			this.VisitSource(join.Right);

			if (join.JoinCondition != null)
			{
				using (this.AcquireIndentationContext())
				{
					this.Write("ON ");

					this.Visit(join.JoinCondition);
				}
			}

			return join;
		}

		protected override Expression VisitTable(SqlTableExpression expression)
		{
			this.WriteTableName(expression.Name);

			return expression;
		}

		protected override Expression VisitSource(Expression source)
		{
			switch ((SqlExpressionType)source.NodeType)
			{
			case SqlExpressionType.Table:
				var table = (SqlTableExpression)source;

				this.Visit(table);

				if (table.Alias != null)
				{
					this.Write(" AS ");
					this.WriteQuotedIdentifier(table.Alias);
				}

				break;
			case SqlExpressionType.Select:
				var select = (SqlSelectExpression)source;
				this.WriteLine();
				this.Write("(");

				using (this.AcquireIndentationContext())
				{
					this.Visit(select);
					this.WriteLine();
				}

				this.Write(")");
				this.Write(" AS ");
				this.WriteQuotedIdentifier(select.Alias);

				break;
			case SqlExpressionType.Join:
				this.VisitJoin((SqlJoinExpression)source);
				break;
			case SqlExpressionType.Delete:
				this.VisitDelete((SqlDeleteExpression)source);
				break;
			case SqlExpressionType.Update:
				this.VisitUpdate((SqlUpdateExpression)source);
				break;
			case SqlExpressionType.Union:
				this.VisitUnion((SqlUnionExpression)source);
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
			this.AppendFullyQualifiedQuotedTableOrTypeName(tableName, this.Write);
		}

		protected virtual void WriteTypeName(string typeName)
		{
			this.AppendFullyQualifiedQuotedTableOrTypeName(typeName, this.Write);
		}

		protected override Expression VisitDelete(SqlDeleteExpression deleteExpression)
		{
			this.Write("DELETE ");
			this.Write("FROM ");
			this.Visit(deleteExpression.Source);

			if (deleteExpression.Where != null)
			{
				this.WriteLine();
				this.Write(" WHERE ");
				this.WriteLine();
				this.Visit(deleteExpression.Where);
			}

			this.WriteLine(";");

			return deleteExpression;
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			var declaringType = memberExpression.Member.DeclaringType;

			if (declaringType != null && Nullable.GetUnderlyingType(declaringType) != null)
			{
				return this.Visit(memberExpression.Expression);
			}

			this.Visit(memberExpression.Expression);
			this.Write(".");
			this.Write("Prop(");
			this.Write(memberExpression.Member.Name);
			this.Write(")");

			return memberExpression;
		}

		protected override Expression VisitObjectReference(SqlObjectReferenceExpression objectReferenceExpression)
		{
			this.Write("ObjectReference(");
			this.Write(objectReferenceExpression.Type.Name);
			this.Write(")");

			return objectReferenceExpression;
		}

		protected override Expression VisitTuple(SqlTupleExpression tupleExpression)
		{
			this.Write('(');
			this.WriteDeliminatedListOfItems(tupleExpression.SubExpressions, c => this.Visit(c));
			this.Write(')');

			return tupleExpression;
		}

		protected override Expression VisitCreateIndex(SqlCreateIndexExpression createIndexExpression)
		{
			this.Write("CREATE ");

			if (createIndexExpression.Unique)
			{
				this.Write("UNIQUE ");
			}

			if (createIndexExpression.IfNotExist)
			{
				this.Write("IF NOT EXIST ");
			}

			this.Write("INDEX ");
			this.WriteQuotedIdentifier(createIndexExpression.IndexName);
			this.Write(" ON ");
			this.Visit(createIndexExpression.Table);
			this.Write("(");
			this.WriteDeliminatedListOfItems(createIndexExpression.Columns, c => this.Visit(c));
			this.WriteLine(")");

			if (this.sqlDialect.SupportsCapability(SqlCapability.IndexInclude))
			{
				if (createIndexExpression.IncludedColumns?.Count > 0)
				{
					this.Write(" INCLUDE ");

					this.Write("(");
					this.WriteDeliminatedListOfItems(createIndexExpression.IncludedColumns, c => this.Visit(c));
					this.WriteLine(")");
				}
			}

			if (createIndexExpression.Where != null)
			{
				this.Write(" WHERE ");
				this.Visit(createIndexExpression.Where);
			}

			this.WriteLine(";");

			return createIndexExpression;
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			this.Write("CREATE TABLE ");
			this.Visit(createTableExpression.Table);
			this.WriteLine();
			this.Write("(");

			using (this.AcquireIndentationContext())
			{
				this.WriteDeliminatedListOfItems(createTableExpression.ColumnDefinitionExpressions, c => this.Visit(c), () => this.WriteLine(","));

				if (createTableExpression.ColumnDefinitionExpressions.Count > 0 && createTableExpression.TableConstraints.Count > 0)
				{
					this.Write(",");
				}

				this.WriteLine();
				this.WriteDeliminatedListOfItems(createTableExpression.TableConstraints, c => this.Visit(c), () => this.WriteLine(","));
			}

			this.WriteLine();
			this.WriteLine(");");

			return createTableExpression;
		}

		protected virtual void Write(SqlColumnReferenceAction action)
		{
			switch (action)
			{
			case SqlColumnReferenceAction.Cascade:
				this.Write("CASCADE");
				break;
			case SqlColumnReferenceAction.Restrict:
				this.Write("RESTRICT");
				break;
			case SqlColumnReferenceAction.SetDefault:
				this.Write("SET DEFAULT");
				break;
			case SqlColumnReferenceAction.SetNull:
				this.Write("SET NULL");
				break;
			}
		}

		protected override Expression VisitForeignKeyConstraint(SqlForeignKeyConstraintExpression foreignKeyConstraintExpression)
		{
			if (!foreignKeyConstraintExpression.ConstraintName.IsNullOrEmpty())
			{
				this.Write("CONSTRAINT ");
				this.WriteQuotedIdentifier(foreignKeyConstraintExpression.ConstraintName);
				this.Write(" ");
			}

			this.Write("FOREIGN KEY(");
			this.WriteDeliminatedListOfItems(foreignKeyConstraintExpression.ColumnNames, this.WriteQuotedIdentifier);
			this.Write(") ");

			this.Visit(foreignKeyConstraintExpression.ReferencesColumnExpression);

			return foreignKeyConstraintExpression;
		}

		protected virtual void WriteQuotedIdentifier(string identifierName)
		{
			this.Write(this.identifierQuoteString);
			this.Write(identifierName);
			this.Write(this.identifierQuoteString);
		}

		protected virtual void WriteQuotedString(string value)
		{
			this.Write(this.stringQuote);
			this.Write(value);
			this.Write(this.stringQuote);
		}


		public virtual void WriteQuotedStringOrObject(object value)
		{
			var s = value as string;

			if (s != null)
			{
				this.WriteQuotedString(s);
			}
			else
			{
				this.writer.Write(value);
			}
		}

		protected override Expression VisitReferencesColumn(SqlReferencesColumnExpression referencesColumnExpression)
		{
			this.Write("REFERENCES ");
			this.Visit(referencesColumnExpression.ReferencedTable);
			this.Write("(");

			this.WriteDeliminatedListOfItems(referencesColumnExpression.ReferencedColumnNames, this.WriteQuotedIdentifier);

			this.Write(")");

			if (referencesColumnExpression.OnDeleteAction != SqlColumnReferenceAction.NoAction)
			{
				this.Write(" ON DELETE ");
				this.Write(referencesColumnExpression.OnDeleteAction);
			}

			if (referencesColumnExpression.OnUpdateAction != SqlColumnReferenceAction.NoAction)
			{
				this.Write(" ON UPDATE ");

				this.Write(referencesColumnExpression.OnUpdateAction);
			}

			if (this.sqlDialect.SupportsCapability(SqlCapability.Deferrability))
			{
				this.WriteDeferrability(referencesColumnExpression.Deferrability);
			}

			return referencesColumnExpression;
		}

		protected virtual void WriteDeferrability(SqlColumnReferenceDeferrability deferrability)
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

		protected override Expression VisitSimpleConstraint(SqlSimpleConstraintExpression simpleConstraintExpression)
		{
			switch (simpleConstraintExpression.Constraint)
			{
			case SqlSimpleConstraint.DefaultValue:
				if (simpleConstraintExpression.Value != null)
				{
					this.Write("DEFAULT");
					this.Write(" ");
					this.Write(simpleConstraintExpression.Value);
				}
				break;
			case SqlSimpleConstraint.NotNull:
				this.Write("NOT NULL");
				break;
			case SqlSimpleConstraint.AutoIncrement:
			{
				var s = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.AutoIncrement);

				if (!string.IsNullOrEmpty(s))
				{
					this.Write(s);
				}
				
				break;
			}
			case SqlSimpleConstraint.PrimaryKey:
				this.Write("PRIMARY KEY");
				if (simpleConstraintExpression.ColumnNames != null)
				{
					this.Write("(");
					this.WriteDeliminatedListOfItems(simpleConstraintExpression.ColumnNames, this.WriteQuotedIdentifier);
					this.Write(")");
				}
				break;
			case SqlSimpleConstraint.Unique:
				this.Write("UNIQUE");
				if (simpleConstraintExpression.ColumnNames != null)
				{
					this.Write("(");
					this.WriteDeliminatedListOfItems(simpleConstraintExpression.ColumnNames, this.WriteQuotedIdentifier);
					this.Write(")");
				}
				break;
			}

			return simpleConstraintExpression;
		}

		protected override Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			this.WriteQuotedIdentifier(columnDefinitionExpression.ColumnName);
			this.Write(' ');
			this.Visit(columnDefinitionExpression.ColumnType);

			if (columnDefinitionExpression.ConstraintExpressions.Count > 0)
			{
				this.Write(' ');
			}

			this.WriteDeliminatedListOfItems(columnDefinitionExpression.ConstraintExpressions, c => this.Visit(c), " ");

			return columnDefinitionExpression;
		}

		protected override Expression VisitConstraintAction(SqlConstraintActionExpression actionExpression)
		{
			this.Write(actionExpression.ActionType.ToString().ToUpper());
			this.Write(" ");
			this.Visit(actionExpression.ConstraintExpression);

			return actionExpression;
		}

		protected override Expression VisitAlterTable(SqlAlterTableExpression alterTableExpression)
		{
			this.Write("ALTER TABLE ");
			this.Visit(alterTableExpression.Table);
			this.Write(" ");

			if (alterTableExpression.ConstraintActions != null)
			{
				this.VisitExpressionList(alterTableExpression.ConstraintActions);
			}
			else if (alterTableExpression.Actions != null)
			{
				foreach (var action in alterTableExpression.Actions)
				{
					this.Visit(action);
				}
			}

			this.WriteLine(";");

			return alterTableExpression;
		}

		protected virtual bool WriteInsertIntoAfterSource(SqlInsertIntoExpression expression)
		{
			return false;
		}

		protected override Expression VisitInsertInto(SqlInsertIntoExpression expression)
		{
			this.Write("INSERT INTO ");
			this.Visit(expression.Source);

			if ((expression.ValueExpressions == null || expression.ValueExpressions.Count == 0) && expression.ValuesExpression == null)
			{
				this.WriteInsertDefaultValuesSuffix();
			}
			else
			{
				this.WriteInsertIntoAfterSource(expression);

				this.Write("(");
				this.WriteDeliminatedListOfItems(expression.ColumnNames, this.WriteQuotedIdentifier);

				this.Write(") ");

				if (this.sqlDialect.SupportsCapability(SqlCapability.InsertOutput))
				{
					this.WriteInsertIntoReturning(expression);
					this.Write(" ");
				}


				if (expression.ValuesExpression != null)
				{
					this.Visit(expression.ValuesExpression);
				}
				else
				{
					this.Write("VALUES (");
					this.WriteDeliminatedListOfItems (expression.ValueExpressions, c =>
					{
						this.Write("(");
						this.Visit(c);
						this.Write(")");
					});
					this.Write(")");
				}
			}

			if (!this.sqlDialect.SupportsCapability(SqlCapability.InsertOutput))
			{
				this.WriteInsertIntoReturning(expression);
			}

			this.Write(";");

			return expression;
		}

		protected override Expression VisitAssign(SqlAssignExpression expression)
		{
			this.Visit(expression.Target);
			this.Write(" = ");
			this.Write("(");
			this.Visit(expression.Value);
			this.Write(")");

			return expression;
		}

		protected override Expression VisitUpdate(SqlUpdateExpression expression)
		{
			this.Write("UPDATE ");
			this.Visit(expression.Source);
			this.Write(" SET ");

			this.WriteDeliminatedListOfItems(expression.Assignments, c => this.Visit(c));

			if (expression.Where == null)
			{
				this.Write(";");
			}

			this.Write(" WHERE ");
			this.Visit(expression.Where);
			this.Write(";");

			return expression;
		}

		protected override Expression VisitCreateType(SqlCreateTypeExpression expression)
		{
			this.Write("CREATE TYPE ");
			this.Visit(expression.SqlType);
			this.Write(" AS ");

			this.Visit(expression.AsExpression);

			this.WriteLine(";");

			return expression;
		}

		protected override Expression VisitEnumDefinition(SqlEnumDefinitionExpression expression)
		{
			this.Write("ENUM (");
			this.WriteDeliminatedListOfItems(expression.Labels, this.WriteQuotedString);
			this.Write(")");

			return expression;
		}

		protected override Expression VisitType(SqlTypeExpression expression)
		{
			if (expression.UserDefinedType)
			{
				this.WriteQuotedIdentifier(expression.TypeName);
			}
			else
			{
				this.Write(expression.TypeName);
			}

			return expression;
		}

		protected override Expression VisitStatementList(SqlStatementListExpression statementListExpression)
		{
			var i = 0;

			foreach (var statement in statementListExpression.Statements)
			{
				this.Visit(statement);

				if (i != statementListExpression.Statements.Count - 1)
				{
					if (statement is SqlSelectExpression)
					{
						this.WriteLine(";");
					}
					else
					{
						this.WriteLine();
					}
				}
			}

			return statementListExpression;
		}

		protected override Expression VisitIndexedColumn(SqlIndexedColumnExpression indexedColumnExpression)
		{
			this.Visit(indexedColumnExpression.Column);

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

		protected override Expression VisitPragma(SqlPragmaExpression expression)
		{
			this.Write("PRAGMA ");
			this.Write(expression.Directive);
			this.WriteLine(";");

			return expression;
		}

		protected override Expression VisitKeyword(SqlKeywordExpression expression)
		{
			this.Write(' ');
			this.Write(expression.Name);
			this.Write(' ');

			return expression;
		}

		protected override Expression VisitSetCommand(SqlSetCommandExpression expression)
		{
			this.Write("SET ");

			if (expression.ConfigurationParameter != null)
			{
				this.Write(expression.ConfigurationParameter);
			}

			if (expression.Target != null)
			{
				this.Write(" ");
				this.Visit(expression.Target);
				this.Write(" ");
			}

			this.Write(" ");
			this.WriteDeliminatedListOfItems(expression.Arguments, c => this.Visit(c));
			this.WriteLine(";");
			
			return expression;
		}

		protected override Expression VisitUnion(SqlUnionExpression expression)
		{
			var savedSelectNext = this.selectNest;
			this.selectNest = 0;

			this.Write("(");
			this.Visit(expression.Left);
			if (expression.UnionAll)
			{
				this.Write(" UNION ALL ");
			}
			else
			{
				this.Write(" UNION ");
			}
			this.Visit(expression.Right);
			this.Write(")");
			this.Write(" AS ");
			this.WriteQuotedIdentifier(expression.Alias);

			this.selectNest = savedSelectNext;

			return expression;
		}
	}
}