// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlRedundantFunctionCallRemover
		: SqlExpressionVisitor
	{
		public static Expression Remove(Expression expression)
		{
			return new SqlRedundantFunctionCallRemover().Visit(expression);		
		}
		
		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (functionCallExpression.Function == SqlFunction.IsNull)
			{
				if (functionCallExpression.Arguments[0].NodeType == ExpressionType.Constant)
				{
					return Expression.Constant(((ConstantExpression) functionCallExpression.Arguments[0]).Value == null);
				}

				return functionCallExpression;
			}
			else if (functionCallExpression.Function == SqlFunction.IsNotNull)
			{
				if (functionCallExpression.Arguments[0].NodeType == ExpressionType.Constant)
				{
					return Expression.Constant(((ConstantExpression)functionCallExpression.Arguments[0]).Value != null);
				}

				return functionCallExpression;
			}
			else if (functionCallExpression.Function == SqlFunction.In)
			{
				var value = this.Visit(functionCallExpression.Arguments[1]) as ConstantExpression;
				
				var sqlValuesEnumerable = value?.Value as SqlValuesEnumerable;

				if (sqlValuesEnumerable?.IsEmpty() == true)
				{
					return Expression.Constant(false);
				}

				return functionCallExpression;	
			}
			else if (functionCallExpression.Function == SqlFunction.Concat)
			{
				var visitedArguments = this.VisitExpressionList(functionCallExpression.Arguments);
				
				if (visitedArguments.All(c => c is ConstantExpression))
				{
					string result;
					
					switch (functionCallExpression.Arguments.Count)
					{
					case 2:
						result = string.Concat((string)((ConstantExpression)visitedArguments[0]).Value, (string)((ConstantExpression)visitedArguments[1]).Value);
						break;
					case 3:
						result = string.Concat((string)((ConstantExpression)visitedArguments[0]).Value, (string)((ConstantExpression)visitedArguments[1]).Value, (string)((ConstantExpression)visitedArguments[2]).Value);
						break;
					case 4:
						result = string.Concat((string)((ConstantExpression)visitedArguments[0]).Value, (string)((ConstantExpression)visitedArguments[1]).Value, (string)((ConstantExpression)visitedArguments[2]).Value, (string)((ConstantExpression)visitedArguments[3]).Value);
						break;
					default:
						result = visitedArguments
								.Cast<ConstantExpression>()
								.Select(c => c.Value)
								.Aggregate(new StringBuilder(), (s, c) => s.Append(c))
								.ToString();
						break;
					}

					return Expression.Constant(result);
				}
			}
			
			return base.VisitFunctionCall(functionCallExpression);
		}
	}
}
