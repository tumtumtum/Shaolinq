// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using Platform;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class Sql92QueryFormatter
		: SqlQueryFormatter
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

		public Expression Expression { get; private set; }

		private readonly SqlQueryFormatterOptions options;
		protected readonly SqlDataTypeProvider sqlDataTypeProvider;
		
		public IndentationContext AcquireIndentationContext()
		{
			return new IndentationContext(this);
		}


		public Sql92QueryFormatter()
			: this(SqlQueryFormatterOptions.Default, null, null)
		{
		}

		public Sql92QueryFormatter(SqlQueryFormatterOptions options)
			: this(options, null, null)
		{
		}

		public Sql92QueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider)
			: base(sqlDialect, new StringWriter(new StringBuilder()))
		{
			this.options = options;

			if (sqlDataTypeProvider == null)
			{
				this.sqlDataTypeProvider = new DefaultSqlDataTypeProvider(ConstraintDefaults.Default);
			}
			else
			{
				this.sqlDataTypeProvider = sqlDataTypeProvider;
			}

			this.identifierQuoteString = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.IdentifierQuote);
		}

		protected override Expression PreProcess(Expression expression)
		{
			expression = base.PreProcess(expression);
			
			if (this.sqlDialect.SupportsFeature(SqlFeature.AlterTableAddConstraints))
			{
				expression = SqlForeignKeyConstraintToAlterAmmender.Ammend(expression);
			}

			return expression;
		}

		protected virtual void WriteInsertDefaultValuesSuffix()
		{
		}

		protected virtual void WriteInsertIntoReturning(SqlInsertIntoExpression expression)
		{
			if (expression.ReturningAutoIncrementColumnName == null)
			{
				return;
			}

			this.Write(" RETURNING ");
			this.WriteQuotedIdentifier(expression.ReturningAutoIncrementColumnName);
		}

		public virtual void AppendFullyQualifiedQuotedTableOrTypeName(string tableName, Action<string> append)
		{
			append(this.identifierQuoteString);
			append(tableName);
			append(this.identifierQuoteString);
		}

		private bool currentProjectShouldBeDefaultIfEmpty;

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var previousCurrentProjectShouldBeDefaultIfEmpty = this.currentProjectShouldBeDefaultIfEmpty;

			currentProjectShouldBeDefaultIfEmpty = projection.IsDefaultIfEmpty;

			var retval = Visit(projection.Select);

			this.currentProjectShouldBeDefaultIfEmpty = previousCurrentProjectShouldBeDefaultIfEmpty;

			return retval;
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
			else if (methodCallExpression.Method.DeclaringType.IsGenericType
			         && methodCallExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>)
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
					this.Write("NOT (");
					Visit(unaryExpression.Operand);
					this.Write(")");
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
				case SqlFunction.Coalesce:
					return new FunctionResolveResult("COALESCE", false, arguments);
				case SqlFunction.Like:
					return new FunctionResolveResult(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Like), true, arguments);
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
					return new FunctionResolveResult("NOT " + this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Like), true, arguments);
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

					return new FunctionResolveResult(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Like), true, new ReadOnlyCollection<Expression>(list));
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

					return new FunctionResolveResult(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Like), true, new ReadOnlyCollection<Expression>(list));
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

					return new FunctionResolveResult(this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Like), true, new ReadOnlyCollection<Expression>(list));
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
				this.Write("(");

				if (result.functionPrefix != null)
				{
					this.Write(result.functionPrefix);
					this.Write(' ');
				}
				
				for (int i = 0, n = result.arguments.Count - 1; i <= n; i++)
				{
					var requiresGrouping = result.arguments[i] is SqlSelectExpression;

					if (requiresGrouping)
					{
						this.Write("(");
					}

					Visit(result.arguments[i]);

					if (requiresGrouping)
					{
						this.Write(")");
					}

					if (i != n)
					{
						this.Write(' ');
						this.Write(result.functionName);
						this.Write(' ');
					}
				}

				if (result.functionSuffix != null)
				{
					this.Write(' ');
					this.Write(result.functionSuffix);
				}

				this.Write(")");
			}
			else
			{
				this.Write(result.functionName);
				this.Write("(");

				if (result.functionPrefix != null)
				{
					this.Write(result.functionPrefix);
					this.Write(' ');
				}

				if (result.argsBefore != null && result.argsBefore.Length > 0)
				{
					for (int i = 0, n = result.argsBefore.Length - 1; i <= n; i++)
					{
						this.Write(this.ParameterIndicatorPrefix);
						this.Write("param");
						this.Write(parameterValues.Count);
						parameterValues.Add(new Pair<Type, object>(result.argsBefore[i].Left, result.argsBefore[i].Right));

						if (i != n || (functionCallExpression.Arguments.Count > 0))
						{
							this.Write(", ");
						}
					}
				}

				for (int i = 0, n = result.arguments.Count - 1; i <= n; i++)
				{
					Visit(result.arguments[i]);

					if (i != n || (result.argsAfter != null && result.argsAfter.Length > 0))
					{
						this.Write(", ");
					}
				}

				if (result.argsAfter != null && result.argsAfter.Length > 0)
				{
					for (int i = 0, n = result.argsAfter.Length - 1; i <= n; i++)
					{
						Write(this.ParameterIndicatorPrefix);
						Write("param");
						Write(parameterValues.Count);
						parameterValues.Add(new Pair<Type, object>(result.argsAfter[i].Left, result.argsAfter[i].Right));

						if (i != n)
						{
							this.Write(", ");
						}
					}
				}

				if (result.functionSuffix != null)
				{
					this.Write(' ');
					this.Write(result.functionSuffix);
				}

				this.Write(")");
			}

			return functionCallExpression;
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			Write("(");

			Visit(binaryExpression.Left);

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
				case ExpressionType.Assign:
					Write(" = ");
					break;
				default:
					throw new NotSupportedException(String.Format("The binary operator '{0}' is not supported", binaryExpression.NodeType));
			}

			Visit(binaryExpression.Right);

			Write(")");

			return binaryExpression;
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholderExpression)
		{
			if ((this.options & SqlQueryFormatterOptions.EvaluateConstantPlaceholders) != 0)
			{
				return base.VisitConstantPlaceholder(constantPlaceholderExpression);
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
					this.Write("NULL");
				}
				else
				{
					this.Write(this.ParameterIndicatorPrefix);
					this.Write("param");
					this.Write(parameterValues.Count);
					parameterValues.Add(new Pair<Type, object>(constantExpression.Type, null));
				}
			}
			else
			{
				var type = constantExpression.Value.GetType();

				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Boolean:
						this.Write(this.ParameterIndicatorPrefix);
						this.Write("param");
						this.Write(parameterValues.Count);
						parameterValues.Add(new Pair<Type, object>(typeof(bool), Convert.ToBoolean(constantExpression.Value)));
						break;
					case TypeCode.Object:
						if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
						{
							this.Write("(");
							this.WriteDeliminatedListOfItems((IEnumerable)constantExpression.Value, c => this.VisitConstant(Expression.Constant(c)));
							this.Write(")");
						}
						else
						{
							this.Write(this.ParameterIndicatorPrefix);
							this.Write("param");
							this.Write(parameterValues.Count);

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
						break;
					default:
						if (constantExpression.Type.IsEnum)
						{
							this.Write(this.ParameterIndicatorPrefix);
							this.Write("param");
							this.Write(parameterValues.Count);

							parameterValues.Add(new Pair<Type, object>(typeof(string), Enum.GetName(constantExpression.Type, constantExpression.Value)));
						}
						else
						{
							this.Write(this.ParameterIndicatorPrefix);
							this.Write("param");
							this.Write(parameterValues.Count);

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
			this.Write(GetAggregateName(sqlAggregate.AggregateType));

			this.Write("(");

			if (sqlAggregate.IsDistinct)
			{
				Write("DISTINCT ");
			}

			if (sqlAggregate.Argument != null)
			{
				this.Visit(sqlAggregate.Argument);
			}
			else if (RequiresAsteriskWhenNoArgument(sqlAggregate.AggregateType))
			{
				this.Write("*");
			}

			this.Write(")");

			return sqlAggregate;
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			this.Write("(");

			using (AcquireIndentationContext())
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
				if (ignoreAlias == columnExpression.SelectAlias)
				{
					this.WriteQuotedIdentifier(replaceAlias);
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

		protected virtual void VisitColumn(SqlSelectExpression selectExpression, SqlColumnDeclaration column)
		{
			var c = Visit(column.Expression) as SqlColumnExpression;

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
			var selectNested = selectNest > 0;

			if (selectNested)
			{
				this.Write("(");
			}

			try
			{
				selectNest++;

				this.Write("SELECT ");

				if (selectExpression.Distinct)
				{
					this.Write("DISTINCT ");
				}

				if (selectExpression.Columns.Count == 0)
				{
					this.Write("* ");
				}

				for (int i = 0, n = selectExpression.Columns.Count; i < n; i++)
				{
					var column = selectExpression.Columns[i];

					if (i > 0)
					{
						this.Write(", ");
					}

					VisitColumn(selectExpression, column);
				}

				if (selectExpression.From != null)
				{
					this.WriteLine();
					this.Write("FROM ");
					VisitSource(selectExpression.From);
				}

				if (selectExpression.Where != null)
				{
					this.WriteLine();
					this.Write("WHERE ");
					Visit(selectExpression.Where);
				}

				if (selectExpression.OrderBy != null && selectExpression.OrderBy.Count > 0)
				{
					this.WriteLine();
					this.Write("ORDER BY ");


					this.WriteDeliminatedListOfItems<Expression>(selectExpression.OrderBy, c =>
					{
						this.Visit(c);

						if (((SqlOrderByExpression)c).OrderType == OrderType.Descending)
						{
							this.Write(" DESC");
						}

						return c;
					});
				}

				if (selectExpression.GroupBy != null && selectExpression.GroupBy.Count > 0)
				{
					this.WriteLine();
					this.Write("GROUP BY ");

					this.WriteDeliminatedListOfItems(selectExpression.GroupBy, this.Visit);
				}

				AppendLimit(selectExpression);

				if (selectExpression.ForUpdate && this.sqlDialect.SupportsFeature(SqlFeature.Constraints))
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
				selectNest--;
			}

			return selectExpression;
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
					Visit(selectExpression.Skip);
				}

				if (selectExpression.Take != null)
				{
					this.Write(", ");

					Visit(selectExpression.Take);
				}
				else if (selectExpression.Skip != null)
				{
					this.Write(", ");
					this.Write(Int64.MaxValue);
				}
			}
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			this.VisitSource(join.Left);

			this.WriteLine();

			switch (join.JoinType)
			{
				case SqlJoinType.CrossJoin:
					this.Write(" CROSS JOIN ");
					break;
				case SqlJoinType.InnerJoin:
					this.Write(" INNER JOIN ");
					break;
				case SqlJoinType.LeftJoin:
					this.Write(" LEFT JOIN ");
					break;
				case SqlJoinType.RightJoin:
					this.Write(" RIGHT JOIN ");
					break;
				case SqlJoinType.OuterJoin:
					this.Write(" FULL OUTER JOIN ");
					break;
			}

			this.VisitSource(join.Right);

			if (join.Condition != null)
			{
				using (AcquireIndentationContext())
				{
					this.Write("ON ");

					this.Visit(join.Condition);
				}
			}

			return join;
		}

		protected override Expression VisitTable(SqlTableExpression expression)
		{
			this.WriteQuotedIdentifier(expression.Name);

			return expression;
		}

		protected override Expression VisitSource(Expression source)
		{
			switch ((SqlExpressionType)source.NodeType)
			{
				case SqlExpressionType.Table:
					var table = (SqlTableExpression)source;

					this.Visit(table);
					this.Write(" AS ");
					this.WriteQuotedIdentifier(table.Alias);
					
					break;
				case SqlExpressionType.Select:
					var select = (SqlSelectExpression)source;
					this.WriteLine();
					this.Write("(");

					using (AcquireIndentationContext())
					{
						Visit(select);
						this.WriteLine();
					}
					
					this.Write(")");
					this.Write(" AS ");
					this.WriteQuotedIdentifier(select.Alias);
					
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
		protected readonly string identifierQuoteString;

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
			this.WriteTableName(deleteExpression.TableName);
			this.WriteLine();
			this.Write(" WHERE ");
			this.WriteLine();

			ignoreAlias = deleteExpression.Alias;
			replaceAlias = deleteExpression.TableName;

			Visit(deleteExpression.Where);

			ignoreAlias = "";

			return deleteExpression;
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			this.Visit(memberExpression.Expression);
			this.Write(".");
			this.Write("Prop(");
			this.Write(memberExpression.Member.Name);
			this.Write(")");

			return memberExpression;
		}

		protected override Expression VisitObjectOperand(SqlObjectOperand objectOperand)
		{
			this.Write("Obj(");
			this.Write(objectOperand.Type.Name);
			this.Write(")");

			return objectOperand;
		}

		protected override Expression VisitTuple(SqlTupleExpression tupleExpression)
		{
			this.Write('(');
			this.WriteDeliminatedListOfItems(tupleExpression.SubExpressions, this.Visit);
			this.Write(')');

			return tupleExpression;
		}

		protected override Expression VisitCreateIndex(SqlCreateIndexExpression createIndexExpression)
		{
			this.Write("CREATE INDEX ");
			this.WriteQuotedIdentifier(createIndexExpression.IndexName);
			this.Write(" ON ");
			this.Visit(createIndexExpression.Table);
			this.Write("(");
			this.WriteDeliminatedListOfItems(createIndexExpression.Columns, this.Visit);
			this.WriteLine(");");
			this.WriteLine();

			return base.VisitCreateIndex(createIndexExpression);
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			this.Write("CREATE TABLE ");
			this.Visit(createTableExpression.Table);
			this.WriteLine();
			this.Write("(");
			
			using (AcquireIndentationContext())
			{
				var i = 0;

				foreach (var expression in createTableExpression.ColumnDefinitionExpressions)
				{
					this.Visit(expression);

					if (i != createTableExpression.ColumnDefinitionExpressions.Count - 1
						|| createTableExpression.TableConstraints.Count > 0)
					{
						this.Write(",");
						this.WriteLine();
					}

					i++;
				}

				this.WriteDeliminatedListOfItems(createTableExpression.TableConstraints, this.Visit);
			}

			this.WriteLine();
			this.WriteLine(");");
			this.WriteLine();

			return createTableExpression;
		}

		private void Write(SqlColumnReferenceAction action)
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
			if (foreignKeyConstraintExpression.ConstraintName != null)
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

		protected virtual string WriteQuotedIdentifier(string identifierName)
		{
			this.Write(identifierQuoteString);
			this.Write(identifierName);
			this.Write(identifierQuoteString);

			return identifierName;
		}

		protected override Expression VisitReferencesColumn(SqlReferencesColumnExpression referencesColumnExpression)
		{
			this.Write("REFERENCES ");
			this.WriteTableName(referencesColumnExpression.ReferencedTableName);
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

			this.WriteDeferrability(referencesColumnExpression.Deferrability);

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
					this.Write(" INITIALLY DEFERRED");
					break;
				case SqlColumnReferenceDeferrability.InitiallyImmediate:
					this.Write(" INITIALLY IMMEDIATE");
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
						this.Write(simpleConstraintExpression.Value);
					}
					break;
				case SqlSimpleConstraint.NotNull:
					this.Write("NOT NULL");
					break;
				case SqlSimpleConstraint.PrimaryKey:
				case SqlSimpleConstraint.PrimaryKeyAutoIncrement:
					this.Write("PRIMARY KEY");
					if (simpleConstraintExpression.ColumnNames != null)
					{
						this.WriteDeliminatedListOfItems(simpleConstraintExpression.ColumnNames, this.WriteQuotedIdentifier);
					}

					if (simpleConstraintExpression.Constraint == SqlSimpleConstraint.PrimaryKeyAutoIncrement)
					{
						var s = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.AutoIncrement);

						if (!string.IsNullOrEmpty(s))
						{
							this.Write(" " + this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.AutoIncrement));
						}
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
			this.Write(columnDefinitionExpression.ColumnTypeName);

			if (columnDefinitionExpression.ConstraintExpressions.Count > 0)
			{
				this.Write(' ');
			}

			this.WriteDeliminatedListOfItems(columnDefinitionExpression.ConstraintExpressions, this.Visit, " ");

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
			this.VisitExpressionList(alterTableExpression.Actions);
			this.WriteLine(";");

			return alterTableExpression;
		}

		protected override Expression VisitInsertInto(SqlInsertIntoExpression expression)
		{
			this.Write("INSERT INTO ");
			this.WriteTableName(expression.TableName);
			this.Write("(");
			this.WriteDeliminatedListOfItems(expression.ColumnNames,this.WriteQuotedIdentifier);

			if (expression.ValueExpressions == null || expression.ValueExpressions.Count == 0)
			{
				this.Write(")");
				this.WriteInsertDefaultValuesSuffix();
				this.Write(",");

				return expression;
			}

			this.Write(") VALUES (");
			this.WriteDeliminatedListOfItems(expression.ValueExpressions, this.Visit);
			this.Write(")");

			this.WriteInsertIntoReturning(expression);
			this.Write(";");

			return expression;
		}

		protected override Expression VisitAssign(SqlAssignExpression expression)
		{
			this.Visit(expression.Target);
			this.Write(" = ");
			this.Visit(expression.Value);

			return expression;
		}

		protected override Expression VisitUpdate(SqlUpdateExpression expression)
		{
			this.Write("UPDATE ");
			this.WriteTableName(expression.TableName);
			this.Write(" SET ");

			this.WriteDeliminatedListOfItems(expression.Assignments, this.Visit);

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

			this.Write(");");

			return expression;
		}

		protected override Expression VisitEnumDefinition(SqlEnumDefinitionExpression expression)
		{
			this.Write("ENUM (");
			this.WriteDeliminatedListOfItems(expression.Labels, this.WriteQuotedIdentifier);
			this.Write(")");

			return expression;
		}

		protected override Expression VisitType(SqlTypeExpression expression)
		{
			this.WriteTypeName(expression.TypeName);

			return expression;
		}
	}
}
