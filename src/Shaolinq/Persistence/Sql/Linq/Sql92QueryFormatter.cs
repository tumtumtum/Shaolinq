using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Sql.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Optimizer;
using Platform;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class Sql92QueryFormatter : SqlQueryFormatter
	{
		public struct FunctionResolveResult
		{
			public static Pair<Type, object>[] MakeArguments(params object[] args)
			{
				var retval = new Pair<Type, object>[args.Length];

				for (var i = 0; i < args.Length; i++)
				{
					retval[i] = new Pair<Type, object>(args[i].GetType(), args[i]);
				}

				return retval;
			}

			public string functionName;
			public bool treatAsOperator;
			public string functionPrefix;
			public string functionSuffix;
			public Pair<Type, object>[] argsAfter;
			public Pair<Type, object>[] argsBefore;
			public ReadOnlyCollection<Expression> arguments;

			public FunctionResolveResult(string functionName, bool treatAsOperator, ReadOnlyCollection<Expression> arguments)
				: this(functionName, treatAsOperator, null, null, arguments)
			{
			}

			public FunctionResolveResult(string functionName, bool treatAsOperator, Pair<Type, object>[] argsBefore, Pair<Type, object>[] argsAfter, ReadOnlyCollection<Expression> arguments)
			{
				this.functionPrefix = null;
				this.functionSuffix = null;
				this.functionName = functionName;
				this.treatAsOperator = treatAsOperator;
				this.argsBefore = argsBefore;
				this.argsAfter = argsAfter;
				this.arguments = arguments;
			}
		}
        
		protected enum Indentation
		{
			Same,
			Inner,
			Outer
		}

		public Expression Expression
		{
			get;
			private set;
		}

		protected virtual char ParameterIndicatorChar
		{
			get
			{
				return '@';
			}
		}

		private int depth;
		protected StringBuilder commandText;
		protected List<Pair<Type, object>> parameterValues;
		private readonly SqlQueryFormatterOptions options;
		protected readonly SqlDataTypeProvider sqlDataTypeProvider;
		protected readonly SqlDialect sqlDialect;

		internal int IndentationWidth
		{
			get;
			private set;
		}

		public Sql92QueryFormatter(Expression expression)
			: this(expression, SqlQueryFormatterOptions.Default, null, null)
		{
		}

		public Sql92QueryFormatter(Expression expression, SqlQueryFormatterOptions options)
			: this(expression, options, null, null)
		{
		}

		public Sql92QueryFormatter(Expression expression, SqlQueryFormatterOptions options, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect)
		{
			this.options = options;

			if (sqlDataTypeProvider == null)
			{
				this.sqlDataTypeProvider = DefaultSqlDataTypeProvider.Instance;
			}
			else
			{
				this.sqlDataTypeProvider = sqlDataTypeProvider;
			}

			if (sqlDialect == null)
			{
				this.sqlDialect = SqlDialect.Default;
			}
			else
			{
				this.sqlDialect = sqlDialect;
			}

			this.IndentationWidth = 2;
			this.Expression = expression;
		}

		public override SqlQueryFormatResult Format()
		{
			if (this.commandText == null)
			{
				commandText = new StringBuilder(512);
				parameterValues = new List<Pair<Type, object>>();

				Visit(this.Expression);
			}

			return new SqlQueryFormatResult(commandText.ToString(), parameterValues);
		}
        
		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			return Visit(projection.Select);
		}

		private void AppendNewLine(Indentation style)
		{
			commandText.AppendLine();

			if (style == Indentation.Inner)
			{
				depth++;
			}
			else if (style == Indentation.Outer)
			{
				depth--;

				Debug.Assert(depth >= 0);
			}

			commandText.Append(' ', depth*this.IndentationWidth);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method == MethodInfoFastRef.ObjectToStringMethod)
			{
				if (methodCallExpression.Object.Type.IsEnum)
				{
					Visit(methodCallExpression.Object);

					return methodCallExpression;
				}
				else
				{
					Visit(methodCallExpression.Object);

					return methodCallExpression;
				}
			}
			else if (methodCallExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>)
				&& methodCallExpression.Method.Name == "GetValueOrDefault")
			{
				Visit(methodCallExpression.Object);

				return methodCallExpression;
			}

			throw new NotSupportedException(String.Format("The method '{0}' is not supported", methodCallExpression.Method.Name));
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

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			switch (unaryExpression.NodeType)
			{
				case ExpressionType.Convert:

					var unaryType = Nullable.GetUnderlyingType(unaryExpression.Type) ?? unaryExpression.Type;
					var operandType = Nullable.GetUnderlyingType(unaryExpression.Operand.Type) ?? unaryExpression.Operand.Type;
					
					if (unaryType == operandType
						|| (IsNumeric(unaryType) && IsNumeric(operandType))
						|| unaryExpression.Operand.Type.IsDataAccessObjectType())
					{
						Visit(unaryExpression.Operand);
					}
					else
					{
						throw new NotSupportedException(String.Format("The unary operator '{0}' is not supported", unaryExpression.NodeType));
					}
					break;
				case ExpressionType.Not:
					this.commandText.Append("NOT (");
					Visit(unaryExpression.Operand);
					this.commandText.Append(")");
					break;
				default:
					throw new NotSupportedException(String.Format("The unary operator '{0}' is not supported", unaryExpression.NodeType));
			}

			return unaryExpression;
		}

		protected virtual FunctionResolveResult ResolveSqlFunction(SqlFunction function, ReadOnlyCollection<Expression> arguments)
		{
			switch (function)
			{
				case SqlFunction.IsNull:
					return new FunctionResolveResult("", true, arguments)
					{
						functionSuffix = "IS NULL"
					};
				case SqlFunction.IsNotNull:
					return new FunctionResolveResult("", true, arguments)
					{
						functionSuffix = "IS NOT NULL"
					};
				case SqlFunction.In:
					return new FunctionResolveResult("IN", true, arguments);
				case SqlFunction.Like:
					return new FunctionResolveResult(this.sqlDialect.LikeString, true, arguments);
				case SqlFunction.CompareObject:
					var expressionType = (ExpressionType)((ConstantExpression)arguments[0]).Value;
					var args = new Expression[2];
					
					args[0] = arguments[1];
					args[1] = arguments[2];

					switch (expressionType)
					{
						case ExpressionType.LessThan:
							return new FunctionResolveResult("<", true, new ReadOnlyCollection<Expression>(args));
						case ExpressionType.LessThanOrEqual:
							return new FunctionResolveResult("<=", true, new ReadOnlyCollection<Expression>(args));
						case ExpressionType.GreaterThan:
							return new FunctionResolveResult(">", true, new ReadOnlyCollection<Expression>(args));
						case ExpressionType.GreaterThanOrEqual:
							return new FunctionResolveResult(">=", true, new ReadOnlyCollection<Expression>(args));
					}
					throw new InvalidOperationException();
				case SqlFunction.NotLike:
					return new FunctionResolveResult("NOT " + this.sqlDialect.LikeString, true, arguments);
				case SqlFunction.ServerDateTime:
					return new FunctionResolveResult("NOW", false, arguments);
				case SqlFunction.StartsWith:
					{
						Expression newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, arguments[1], Expression.Constant("%"));
						newArgument = RedundantFunctionCallRemover.Remove(newArgument);
						
						var list = new List<Expression>
						{
							arguments[0],
							newArgument
						};

						return new FunctionResolveResult(this.sqlDialect.LikeString, true, new ReadOnlyCollection<Expression>(list));
					}
				case SqlFunction.ContainsString:
					{
						Expression newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, arguments[1], Expression.Constant("%"));
						newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, Expression.Constant("%"), newArgument);
						newArgument = RedundantFunctionCallRemover.Remove(newArgument);
						
						var list = new List<Expression>
						{
							arguments[0],
							newArgument
						};

						return new FunctionResolveResult(this.sqlDialect.LikeString, true, new ReadOnlyCollection<Expression>(list));
					}
				case SqlFunction.EndsWith:
					{
						Expression newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, Expression.Constant("%"), arguments[1]);
						newArgument = RedundantFunctionCallRemover.Remove(newArgument);
						
						var list = new List<Expression>
						{
							arguments[0],
							newArgument
						};

						return new FunctionResolveResult(this.sqlDialect.LikeString, true, new ReadOnlyCollection<Expression>(list));
					}
				default:
					return new FunctionResolveResult(function.ToString().ToUpper(), false, arguments);
			}
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			var result = ResolveSqlFunction(functionCallExpression.Function, functionCallExpression.Arguments);

			if (result.treatAsOperator)
			{
				this.commandText.Append("(");
				
				if (result.functionPrefix != null)
				{
					this.commandText.Append(result.functionPrefix).Append(' ');
				}

				for (int i = 0, n = result.arguments.Count - 1; i <= n; i++)
				{
					var requiresGrouping = result.arguments[i] is SqlSelectExpression;

					if (requiresGrouping)
					{
						this.commandText.Append("(");
					}

					Visit(result.arguments[i]);

					if (requiresGrouping)
					{
						this.commandText.Append(")");
					}

					if (i != n)
					{
						this.commandText.Append(' ');
						this.commandText.Append(result.functionName);
						this.commandText.Append(' ');
					}
				}

				if (result.functionSuffix != null)
				{
					this.commandText.Append(' ').Append(result.functionSuffix);
				}

				this.commandText.Append(")");
			}
			else
			{
				this.commandText.Append(result.functionName);
				this.commandText.Append("(");

				if (result.functionPrefix != null)
				{
					this.commandText.Append(result.functionPrefix).Append(' ');
				}
                
				if (result.argsBefore != null && result.argsBefore.Length > 0)
				{
					for (int i = 0, n = result.argsBefore.Length - 1; i <= n; i++)
					{
						commandText.Append(this.ParameterIndicatorChar);
						commandText.Append("param");
						commandText.Append(parameterValues.Count);
						parameterValues.Add(new Pair<Type, object>(result.argsBefore[i].Left, result.argsBefore[i].Right));

						if (i != n || (functionCallExpression.Arguments.Count > 0))
						{
							this.commandText.Append(", ");
						}
					}
				}

				for (int i = 0, n = result.arguments.Count - 1; i <= n; i++)
				{
					Visit(result.arguments[i]);
					
					if (i != n || (result.argsAfter != null && result.argsAfter.Length > 0))
					{
						this.commandText.Append(", ");
					}
				}

				if (result.argsAfter != null && result.argsAfter.Length > 0)
				{
					for (int i = 0, n = result.argsAfter.Length - 1; i <= n; i++)
					{
						commandText.Append(this.ParameterIndicatorChar);
						commandText.Append("param");
						commandText.Append(parameterValues.Count);
						parameterValues.Add(new Pair<Type, object>(result.argsAfter[i].Left, result.argsAfter[i].Right));

						if (i != n)
						{
							this.commandText.Append(", ");
						}
					}
				}

				if (result.functionSuffix != null)
				{
					this.commandText.Append(' ').Append(result.functionSuffix);
				}

				this.commandText.Append(")");
			}

			return functionCallExpression;
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
            commandText.Append("(");

            Visit(binaryExpression.Left);

            switch (binaryExpression.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    commandText.Append(" AND ");
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    commandText.Append(" OR ");
                    break;
                case ExpressionType.Equal:
                    commandText.Append(" = ");
                    break;
                case ExpressionType.NotEqual:
                    commandText.Append(" <> ");
                    break;
                case ExpressionType.LessThan:
                    commandText.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    commandText.Append(" <= ");
                    break;
                case ExpressionType.GreaterThan:
                    commandText.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    commandText.Append(" >= ");
                    break;
                case ExpressionType.Add:
                    commandText.Append(" + ");
                    break;
                case ExpressionType.Subtract:
                    commandText.Append(" - ");
                    break;
                case ExpressionType.Multiply:
                    commandText.Append(" * ");
                    break;
                default:
                    throw new NotSupportedException(String.Format("The binary operator '{0}' is not supported", binaryExpression.NodeType));
            }

            Visit(binaryExpression.Right);

            commandText.Append(")");

			return binaryExpression;
		}

		protected virtual void VisitCollection(IEnumerable collection)
		{
			var i = 0;

			commandText.Append("(");

			foreach (var obj in collection)
			{
				VisitConstant(Expression.Constant(obj));

				commandText.Append(", ");
				i++;
			}

			if (i > 0)
			{
				commandText.Length -= 2;
			}

			commandText.Append(")");
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholderExpression)
		{
			if (this.options.EvaluateConstantPlaceholders)
			{
				return base.VisitConstantPlaceholder(constantPlaceholderExpression);
			}
			else
			{
				commandText.AppendFormat("$${0}", constantPlaceholderExpression.Index);

				return constantPlaceholderExpression;
			}
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (constantExpression.Value == null)
			{
				commandText.Append("NULL");
			}
			else
			{
				var type = constantExpression.Value.GetType();

				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Boolean:
						commandText.Append(this.ParameterIndicatorChar);
						commandText.Append("param");
						commandText.Append(parameterValues.Count);
						parameterValues.Add(new Pair<Type, object>(typeof(bool), Convert.ToBoolean(constantExpression.Value)));
						break;
					case TypeCode.Object:
						if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
						{
							VisitCollection((IEnumerable)constantExpression.Value);
						}
						else if (type == typeof(Guid))
						{
							commandText.Append(this.ParameterIndicatorChar);
							commandText.Append("param");
							commandText.Append(parameterValues.Count);

							var value = constantExpression.Value as Guid?;

							if (this.sqlDataTypeProvider != null)
							{
								parameterValues.Add(this.sqlDataTypeProvider.GetSqlDataType(constantExpression.Type).ConvertForSql(value));
							}
							else
							{
								parameterValues.Add(new Pair<Type, object>(constantExpression.Type, value));
							}
						}
						else
						{
							commandText.Append("obj: " + constantExpression.Value);
						}
						break;
					default:
						if (constantExpression.Type.IsEnum)
						{
							commandText.Append(this.ParameterIndicatorChar);
							commandText.Append("param");
							commandText.Append(parameterValues.Count);
							parameterValues.Add(new Pair<Type, object>(typeof(string), Enum.GetName(constantExpression.Type, constantExpression.Value)));
						}
						else
						{
							commandText.Append(this.ParameterIndicatorChar);
							commandText.Append("param");
							commandText.Append(parameterValues.Count);
							parameterValues.Add(new Pair<Type, object>(constantExpression.Type, constantExpression.Value));
						}
						break;
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
				case SqlAggregateType.Min:
					return "MIN";
				case SqlAggregateType.Max:
					return "MAX";
				case SqlAggregateType.Sum:
					return "SUM";
				case SqlAggregateType.Average:
					return "AVG";
				default:
					throw new NotSupportedException(String.Concat("Unknown aggregate type: ", aggregateType));
			}
		}
        
		protected virtual bool RequiresAsteriskWhenNoArgument(SqlAggregateType aggregateType)
		{
			return aggregateType == SqlAggregateType.Count;
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			commandText.Append(GetAggregateName(sqlAggregate.AggregateType));

			commandText.Append("(");

			if (sqlAggregate.IsDistinct)
			{
				commandText.Append("DISTINCT ");
			}

			if (sqlAggregate.Argument != null)
			{
				this.Visit(sqlAggregate.Argument);
			}
			else if (RequiresAsteriskWhenNoArgument(sqlAggregate.AggregateType))
			{
				commandText.Append("*");
			}
			
			commandText.Append(")");

			return sqlAggregate;
		}

		protected void Indent(Indentation style)
		{
			if (style == Indentation.Inner)
			{
				this.depth++;
			}
			else if (style == Indentation.Outer)
			{
				this.depth--;
			}
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			commandText.Append("(");
			this.AppendNewLine(Indentation.Inner);
			this.Visit(subquery.Select);
			this.AppendNewLine(Indentation.Same);
			commandText.Append(")");

			this.Indent(Indentation.Outer);

			return subquery;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (!String.IsNullOrEmpty(columnExpression.SelectAlias))
			{
				if (ignoreAlias == columnExpression.SelectAlias)
				{
					commandText.Append(this.sqlDialect.NameQuoteChar).Append(replaceAlias).Append(this.sqlDialect.NameQuoteChar);
				}
				else
				{
					try
					{
						commandText.Append(this.sqlDialect.NameQuoteChar).Append(columnExpression.SelectAlias).Append(
							this.sqlDialect.NameQuoteChar);
					}
					catch (NullReferenceException)
					{
						Console.WriteLine();
					}
				}

				commandText.Append(".");
			}

			commandText.Append(this.sqlDialect.NameQuoteChar).Append(columnExpression.Name).Append(this.sqlDialect.NameQuoteChar);

			return columnExpression;
		}

		protected virtual void VisitColumn(SqlSelectExpression selectExpression, SqlColumnDeclaration column)
		{
			var c = Visit(column.Expression) as SqlColumnExpression;

			if ((c == null || c.Name != column.Name) && !String.IsNullOrEmpty(column.Name))
			{
				commandText.Append(" AS ");
				commandText.Append(this.sqlDialect.NameQuoteChar).Append(column.Name).Append(this.sqlDialect.NameQuoteChar);
			}
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			commandText.Append("CASE WHEN (");
			Visit(expression.Test);
			commandText.Append(")");
			commandText.Append(" THEN (");
			Visit(expression.IfTrue);
			commandText.Append(") ELSE (");
			Visit(expression.IfFalse);
			commandText.Append(") END");

			return expression;
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			commandText.Append("SELECT ");

			if (selectExpression.Distinct)
			{
				commandText.Append("DISTINCT ");
			}

			if (selectExpression.Columns.Count == 0)
			{
				commandText.Append("* ");
			}

			for (int i = 0, n = selectExpression.Columns.Count; i < n; i++)
			{
				var column = selectExpression.Columns[i];

				if (i > 0)
				{
					commandText.Append(", ");
				}

				VisitColumn(selectExpression, column);
			}

			if (selectExpression.From != null)
			{
				AppendNewLine(Indentation.Same);
				commandText.Append("FROM ");
				VisitSource(selectExpression.From);
			}

			if (selectExpression.Where != null)
			{
				AppendNewLine(Indentation.Same);
				commandText.Append("WHERE ");
				Visit(selectExpression.Where);
			}

			if (selectExpression.OrderBy != null && selectExpression.OrderBy.Count > 0)
			{
				this.AppendNewLine(Indentation.Same);

				commandText.Append("ORDER BY ");

				for (int i = 0; i < selectExpression.OrderBy.Count; i++)
				{
					var orderExpression = selectExpression.OrderBy[i];

					if (i > 0)
					{
						commandText.Append(", ");
					}

					this.Visit(orderExpression.Expression);

					if (orderExpression.OrderType == OrderType.Descending)
					{
						commandText.Append(" DESC");
					}
				}
			}

			if (selectExpression.GroupBy != null && selectExpression.GroupBy.Count > 0)
			{
				this.AppendNewLine(Indentation.Same);
				commandText.Append("GROUP BY ");

				for (var i = 0; i < selectExpression.GroupBy.Count; i++)
				{
					if (i > 0)
					{
						commandText.Append(", ");
					}

					this.Visit(selectExpression.GroupBy[i]);
				}
			}

			AppendLimit(selectExpression);

			if (selectExpression.ForUpdate && this.sqlDialect.SupportsForUpdate)
			{
				commandText.Append(" FOR UPDATE");
			}

			return selectExpression;
		}

		protected virtual void AppendLimit(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null || selectExpression.Take != null)
			{
				commandText.Append(" LIMIT ");

				if (selectExpression.Skip == null)
				{
					commandText.Append("0");
				}
				else
				{
					Visit(selectExpression.Skip);
				}

				if (selectExpression.Take != null)
				{
					commandText.Append(", ");

					Visit(selectExpression.Take);
				}
				else if (selectExpression.Skip != null)
				{
					commandText.Append(", ");
					commandText.Append(Int64.MaxValue);
				}
			}
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			this.VisitSource(join.Left);

			this.AppendNewLine(Indentation.Same);

			switch (join.JoinType)
			{
				case SqlJoinType.CrossJoin:
					commandText.Append(" CROSS JOIN ");
					break;
				case SqlJoinType.InnerJoin:
					commandText.Append(" INNER JOIN ");
					break;
				case SqlJoinType.LeftJoin:
					commandText.Append(" LEFT JOIN ");
					break;
				case SqlJoinType.RightJoin:
					commandText.Append(" RIGHT JOIN ");
					break;
				case SqlJoinType.OuterJoin:
					commandText.Append(" FULL OUTER JOIN ");
					break;
			}

			this.VisitSource(join.Right);

			if (join.Condition != null)
			{
				this.AppendNewLine(Indentation.Inner);
				commandText.Append("ON ");
				this.Visit(join.Condition);
				this.AppendNewLine(Indentation.Outer);
			}
            
			return join;
		}
        
		protected override Expression VisitSource(Expression source)
		{
			switch ((SqlExpressionType)source.NodeType)
			{
				case SqlExpressionType.Table:
					var table = (SqlTableExpression)source;
					commandText.Append(this.sqlDialect.NameQuoteChar);
					commandText.Append(table.Name);
					commandText.Append(this.sqlDialect.NameQuoteChar);
					commandText.Append(" AS ");
					commandText.Append(this.sqlDialect.NameQuoteChar).Append(table.Alias).Append(this.sqlDialect.NameQuoteChar);
					break;
				case SqlExpressionType.Select:
					var select = (SqlSelectExpression)source;
					AppendNewLine(Indentation.Same);
					commandText.Append("(");
					AppendNewLine(Indentation.Inner);
					Visit(select);
					AppendNewLine(Indentation.Outer);
					commandText.Append(")");
					commandText.Append(" AS ");
					commandText.Append(this.sqlDialect.NameQuoteChar).Append(select.Alias).Append(this.sqlDialect.NameQuoteChar);
					break;
				case SqlExpressionType.Join:
					this.VisitJoin((SqlJoinExpression)source);
					break;
				default:
					throw new InvalidOperationException(String.Format("Select source ({0}) is not valid type", source.NodeType));
			}

			return source;
		}

		protected string ignoreAlias;
		protected string replaceAlias;

		protected override Expression VisitDeleteExpression(SqlDeleteExpression deleteExpression)
		{
			commandText.Append("DELETE ");
			commandText.Append("FROM ").Append(this.sqlDialect.NameQuoteChar);
			commandText.Append(deleteExpression.TableName);
			commandText.Append(this.sqlDialect.NameQuoteChar);
			this.AppendNewLine(Indentation.Same);
			commandText.Append(" WHERE ");
			this.AppendNewLine(Indentation.Same);

			ignoreAlias = deleteExpression.Alias;
			replaceAlias = deleteExpression.TableName;

			Visit(deleteExpression.Where);

			ignoreAlias = "";

			return deleteExpression;
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			this.Visit(memberExpression.Expression);
			commandText.Append(".");
			commandText.Append("Prop(");
			commandText.Append(memberExpression.Member.Name);
			commandText.Append(")");

			return memberExpression;
		}

		protected override Expression VisitObjectOperand(SqlObjectOperand objectOperand)
		{
			commandText.Append("Obj(").Append(objectOperand.Type.Name).Append(")");

			return objectOperand;
		}
	}
}