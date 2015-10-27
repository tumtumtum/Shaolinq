// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public static class ExpressionExtensions
	{
		public static LambdaExpression StripQuotes(this Expression expression)
		{
			while (expression.NodeType == ExpressionType.Quote)
			{
				expression = ((UnaryExpression)expression).Operand;
			}

			return (LambdaExpression)expression;
		}

		public static Expression StripDefaultIfEmptyCalls(this Expression expression, out bool didStrip)
		{
			MethodCallExpression methodCallExpression;

			didStrip = false;

            while ((methodCallExpression = (expression as MethodCallExpression)) != null
				&& methodCallExpression.Method.IsGenericMethod && methodCallExpression.Method.GetGenericMethodDefinition() == MethodInfoFastRef.QueryableDefaultIfEmptyMethod)
			{
				expression = methodCallExpression.Arguments[0];

				didStrip = true;
            }

			return expression;
		}
	}
}
