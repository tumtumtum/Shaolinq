// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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
				ConstantExpression constantExpression;

				if ((constantExpression = this.Visit(functionCallExpression.Arguments[0]).StripAndGetConstant()) != null)
				{
					return Expression.Constant(constantExpression.Value == null);
				}

				return functionCallExpression;
			}
			else if (functionCallExpression.Function == SqlFunction.IsNotNull)
			{
				ConstantExpression constantExpression;

				if ((constantExpression = this.Visit(functionCallExpression.Arguments[0]).StripAndGetConstant()) != null)
				{
					return Expression.Constant(constantExpression.Value != null);
				}

				return functionCallExpression;
			}
			else if (functionCallExpression.Function == SqlFunction.In)
			{
				var value = this.Visit(functionCallExpression.Arguments[1]).StripAndGetConstant();
				
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
				
				if (visitedArguments.All(c => c.StripAndGetConstant() != null))
				{
					string result;
					
					switch (functionCallExpression.Arguments.Count)
					{
					case 2:
						result = string.Concat((string)visitedArguments[0].StripAndGetConstant().Value, (string)visitedArguments[1].StripAndGetConstant().Value);
						break;
					case 3:
						result = string.Concat((string)visitedArguments[0].StripAndGetConstant().Value, (string)visitedArguments[1].StripAndGetConstant().Value, (string)visitedArguments[2].StripAndGetConstant().Value);
						break;
					case 4:
						result = string.Concat((string)visitedArguments[0].StripAndGetConstant().Value, (string)visitedArguments[1].StripAndGetConstant().Value, (string)((ConstantExpression)visitedArguments[2]).Value, (string)visitedArguments[3].StripAndGetConstant().Value);
						break;
					default:
						result = visitedArguments
								.Select(c => c.StripAndGetConstant().Value)
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
