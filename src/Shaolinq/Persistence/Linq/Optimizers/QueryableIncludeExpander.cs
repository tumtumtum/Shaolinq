// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Platform.Reflection;
using Shaolinq.TypeBuilding;
using Platform;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class QueryableIncludeExpander
		: SqlExpressionVisitor
	{
		private QueryableIncludeExpander()
		{
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			var queryable = constantExpression.Value as IQueryable;

			if (queryable != null && queryable.Expression != constantExpression)
			{
				return this.Visit(queryable.Expression);
			}

			return base.VisitConstant(constantExpression);
		}

		public static Expression Expand(Expression expression)
		{
			return new QueryableIncludeExpander().Visit(expression);
		}

		private Expression TranslateIncludeCall(MethodCallExpression methodCallExpression)
		{
			var parameter = Expression.Parameter(methodCallExpression.Method.GetGenericArguments()[0]);
			var currentIncludeSelector = this.Visit(methodCallExpression.Arguments[1]).StripQuotes();

			var body = Expression.Call(null, MethodInfoFastRef.DataAccessObjectExtensionsIncludeMethod.MakeGenericMethod(parameter.Type, currentIncludeSelector.ReturnType), parameter, currentIncludeSelector);
			var selector = Expression.Lambda(body, parameter);
			var source = this.Visit(methodCallExpression.Arguments[0]);
			var selectCall = Expression.Call(null, MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod(parameter.Type, parameter.Type), source, selector);

			return selectCall;
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.GetGenericMethodOrRegular() != MethodInfoFastRef.QueryableExtensionsIncludeMethod)
            {
				return base.VisitMethodCall(methodCallExpression);
			}

			var parameter = Expression.Parameter(methodCallExpression.Method.GetGenericArguments()[0]);
			var currentIncludeSelector = this.Visit(methodCallExpression.Arguments[1]).StripQuotes();

			var body = Expression.Call(null, MethodInfoFastRef.DataAccessObjectExtensionsIncludeMethod.MakeGenericMethod(parameter.Type, currentIncludeSelector.ReturnType), parameter, currentIncludeSelector);
			var selector = Expression.Lambda(body, parameter);
			var source = this.Visit(methodCallExpression.Arguments[0]);
			var selectCall = Expression.Call(null, MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod(parameter.Type, parameter.Type), source, selector);

			return selectCall;
		}
	}
}
