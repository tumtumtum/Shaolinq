// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
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

		public static Expression Strip(this Expression expression, Func<Expression, Expression> inner)
		{
			Expression result;

			expression.TryStrip(inner, out result);

			return result;
		}

		public static bool TryStripDefaultIfEmptyCalls(this Expression expression, out Expression retval)
		{
			return expression.TryStrip(c => c.NodeType == ExpressionType.Call
										&& (((c as MethodCallExpression)?.Method.IsGenericMethod ?? false)
										&& ((MethodCallExpression)c).Method.GetGenericMethodDefinition() == MethodInfoFastRef.QueryableDefaultIfEmptyMethod) ? ((MethodCallExpression)c).Arguments[0] : null,
										out retval);
		}

        public static bool TryStrip(this Expression expression, Func<Expression, Expression> inner, out Expression retval)
		{
			var didStrip = false;

            Expression current;
	        var previous = expression;

			while ((current = inner(previous)) != null)
			{
				previous = current;
				didStrip = true;
            }

			retval = previous;

			return didStrip;
		}
	}
}
