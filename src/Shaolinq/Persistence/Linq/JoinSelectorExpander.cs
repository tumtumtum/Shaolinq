// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class JoinSelectorExpander
		: SqlExpressionVisitor
	{
		public static Expression Expand(Expression expression)
		{
			return new JoinSelectorExpander().Visit(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.DeclaringType != typeof(Queryable)
				&& methodCallExpression.Method.DeclaringType != typeof(Enumerable)
				&& methodCallExpression.Method.DeclaringType != typeof(QueryableExtensions))
			{
				return base.VisitMethodCall(methodCallExpression);
			}

			switch (methodCallExpression.Method.Name)
			{
			case "Join":
				return this.RewriteExplicitJoinProjection(methodCallExpression);
			default:
				return base.VisitMethodCall(methodCallExpression);
			}
		}

		protected Expression RewriteExplicitJoinProjection(MethodCallExpression methodCallExpression)
		{
			var outer = this.Visit(methodCallExpression.Arguments[0]);
			var inner = this.Visit(methodCallExpression.Arguments[1]);
			var outerKeySelector = methodCallExpression.Arguments[2].StripQuotes();
			var innerKeySelector = methodCallExpression.Arguments[3].StripQuotes();
			var resultSelector = methodCallExpression.Arguments[4].StripQuotes();

			var originalOuterKeyParam = resultSelector.StripQuotes().Parameters[0];
			var originalInnerKeyParam = resultSelector.StripQuotes().Parameters[1];

			var outerKey = Expression.Parameter(outerKeySelector.Parameters[0].Type);
			var innerKey = Expression.Parameter(innerKeySelector.Parameters[0].Type);
			var resultValue = Expression.Parameter(typeof(ExpandedJoinSelectKey<,>).MakeGenericType(outerKey.Type, innerKey.Type));

			var newResultSelector = Expression.Lambda(Expression.MemberInit(resultValue.Type.CreateNewExpression(), Expression.Bind(resultValue.Type.GetProperty("Outer"), outerKey),  Expression.Bind(resultValue.Type.GetProperty("Inner"), innerKey)), outerKey, innerKey);

			var newJoin = Expression.Call(null, MethodInfoFastRef.QueryableJoinMethod.MakeGenericMethod(outer.Type.GetSequenceElementType() ?? outer.Type, inner.Type.GetSequenceElementType() ?? inner.Type, outerKeySelector.ReturnType, newResultSelector.ReturnType), outer, inner, outerKeySelector, innerKeySelector, newResultSelector);

			var selectorParameter = Expression.Parameter(resultValue.Type);
			var selectProjectorBody = SqlExpressionReplacer.Replace(resultSelector.Body, originalOuterKeyParam, Expression.Property(selectorParameter, "Outer"));

			selectProjectorBody = SqlExpressionReplacer.Replace(selectProjectorBody, originalInnerKeyParam, Expression.Property(selectorParameter, "Inner"));

			var selectProjector = Expression.Lambda(selectProjectorBody, selectorParameter);

			var select = Expression.Call(null, MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod(selectorParameter.Type, selectProjector.ReturnType), newJoin, selectProjector);

			return select;
		}
	}
}
