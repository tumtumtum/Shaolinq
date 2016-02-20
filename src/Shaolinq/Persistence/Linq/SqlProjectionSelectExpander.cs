// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class SqlProjectionSelectExpander
		: SqlExpressionVisitor
	{
		public static Expression Expand(Expression expression)
		{
			return new SqlProjectionSelectExpander().Visit(expression);
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
			case "SelectMany":
				if (methodCallExpression.Arguments.Count < 3)
				{
					goto default;
				}
				return this.RewriteSelectManyProjection(methodCallExpression);
			default:
				return base.VisitMethodCall(methodCallExpression);
			}
		}

		/// <summary>
		/// Translates Joins from type (1) to type (2)
		/// 1) Join(outer, inner, c => c.outerKey, c => d.innerKey, (x, y) => new { x.a.b.c, y.a.b.c })
		/// 2) Join(outer, inner, c => c.outerKey, c => d.innerKey, (x, y) => new { x, y }).Select(c => new { c.x.a.b.c, c.y.a.b.v })
		/// </summary>
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

			var newResultSelector = Expression.Lambda(Expression.MemberInit(resultValue.Type.CreateNewExpression(), Expression.Bind(resultValue.Type.GetProperty("Outer"), outerKey), Expression.Bind(resultValue.Type.GetProperty("Inner"), innerKey)), outerKey, innerKey);

			var newJoin = Expression.Call(MethodInfoFastRef.QueryableJoinMethod.MakeGenericMethod(outer.Type.GetSequenceElementType() ?? outer.Type, inner.Type.GetSequenceElementType() ?? inner.Type, outerKeySelector.ReturnType, newResultSelector.ReturnType), outer, inner, outerKeySelector, innerKeySelector, newResultSelector);

			var selectorParameter = Expression.Parameter(resultValue.Type);
			var selectProjectorBody = SqlExpressionReplacer.Replace(resultSelector.Body, originalOuterKeyParam, Expression.Property(selectorParameter, "Outer"));

			selectProjectorBody = SqlExpressionReplacer.Replace(selectProjectorBody, originalInnerKeyParam, Expression.Property(selectorParameter, "Inner"));

			var selectProjector = Expression.Lambda(selectProjectorBody, selectorParameter);

			var select = Expression.Call(MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod(selectorParameter.Type, selectProjector.ReturnType), newJoin, selectProjector);

			return select;
		}
		
		protected Expression RewriteSelectManyProjection(MethodCallExpression methodCallExpression)
		{
			var outer = this.Visit(methodCallExpression.Arguments[0]);
			var collection = this.Visit(methodCallExpression.Arguments[1]);
			var resultSelector = methodCallExpression.Arguments.Count == 3 ? methodCallExpression.Arguments[2].StripQuotes() : null;

			var originalSelectorA = resultSelector.StripQuotes().Parameters[0];
			var originalSelectorB = resultSelector.StripQuotes().Parameters[1];

			var newA = Expression.Parameter(originalSelectorA.Type);
			var newB = Expression.Parameter(originalSelectorB.Type);

			var resultValue = Expression.Parameter(typeof(ExpandedJoinSelectKey<,>).MakeGenericType(originalSelectorA.Type, originalSelectorB.Type));

			var newResultSelector = Expression.Lambda(Expression.MemberInit(resultValue.Type.CreateNewExpression(), Expression.Bind(resultValue.Type.GetProperty("Outer"), newA), Expression.Bind(resultValue.Type.GetProperty("Inner"), newB)), newA, newB);

			var newSelectMany = Expression.Call(MethodInfoFastRef.QueryableSelectManyMethod.MakeGenericMethod(methodCallExpression.Method.GetGenericArguments()[0], methodCallExpression.Method.GetGenericArguments()[1], newResultSelector.ReturnType), outer, collection, newResultSelector);

			var selectorParameter = Expression.Parameter(resultValue.Type);
			var selectProjectorBody = SqlExpressionReplacer.Replace(resultSelector?.Body ?? selectorParameter, originalSelectorA, Expression.Property(selectorParameter, "Outer"));

			selectProjectorBody = SqlExpressionReplacer.Replace(selectProjectorBody, originalSelectorB, Expression.Property(selectorParameter, "Inner"));

			var selectProjector = Expression.Lambda(selectProjectorBody, selectorParameter);

			var select = Expression.Call(MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod(selectorParameter.Type, selectProjector.ReturnType), newSelectMany, selectProjector);

			return select;
		}
	}
}
