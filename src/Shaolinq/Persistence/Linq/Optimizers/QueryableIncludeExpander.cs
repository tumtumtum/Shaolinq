using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class QueryableIncludeExpander
		: SqlExpressionVisitor
	{
		private QueryableIncludeExpander()
		{
		}

		public static Expression Expand(Expression expression)
		{
			return new QueryableIncludeExpander().Visit(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.IsGenericMethod 
				&& methodCallExpression.Method.GetGenericMethodDefinition() == MethodInfoFastRef.QueryableExtensionsIncludeMethod)
			{
				var parameter = Expression.Parameter(methodCallExpression.Method.GetGenericArguments()[0]);
				var currentIncludeSelector = (LambdaExpression)QueryBinder.StripQuotes(this.Visit(methodCallExpression.Arguments[1]));

				var body = Expression.Call(null, MethodInfoFastRef.DataAccessObjectExtensionsIncludeMethod.MakeGenericMethod(parameter.Type, currentIncludeSelector.ReturnType), parameter, currentIncludeSelector);
				var selector = Expression.Lambda(body, parameter);
				var source = this.Visit(methodCallExpression.Arguments[0]);
				var selectCall = Expression.Call(null, MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod(parameter.Type, parameter.Type), source, selector);

				return selectCall;
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
