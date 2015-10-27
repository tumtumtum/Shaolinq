// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.TypeBuilding;

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
				var currentIncludeSelector = this.Visit(methodCallExpression.Arguments[1]).StripQuotes();

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
