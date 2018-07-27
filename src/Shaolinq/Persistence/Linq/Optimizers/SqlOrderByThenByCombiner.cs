using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlOrderByThenByCombiner
		: SqlExpressionVisitor
	{
		private List<MethodCallExpression> currentThenBys;

		public static Expression Combine(Expression expression)
		{
			return new SqlOrderByThenByCombiner().Visit(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.DeclaringType == typeof(Queryable) && methodCallExpression.Method.IsGenericMethod)
			{
				var method = methodCallExpression.Method.GetGenericMethodDefinition();

				if (method == MethodInfoFastRef.QueryableThenByMethod || method == MethodInfoFastRef.QueryableThenByDescendingMethod)
				{
					if (currentThenBys == null)
					{
						currentThenBys = new List<MethodCallExpression>();
					}

					currentThenBys.Add(methodCallExpression);

					return this.Visit(methodCallExpression.Arguments[0]);
				}

				if ((method == MethodInfoFastRef.QueryableOrderByMethod || method == MethodInfoFastRef.QueryableOrderByDescendingMethod)
					&& this.currentThenBys?.Count > 0)
				{
					var orderByThenBys = new List<LambdaExpression> { methodCallExpression.Arguments[1].StripQuotes() };
					var sortBys = new List<SortOrder> { method == MethodInfoFastRef.QueryableOrderByDescendingMethod ? SortOrder.Descending : SortOrder.Ascending };

					if (this.currentThenBys != null)
					{
						orderByThenBys.AddRange(this.currentThenBys.Select(c => c.Arguments[1].StripQuotes()).Reverse());
						sortBys.AddRange(this.currentThenBys.Select(c => c.Method.GetGenericMethodDefinition() == MethodInfoFastRef.QueryableThenByDescendingMethod ? SortOrder.Descending : SortOrder.Ascending).Reverse());

						this.currentThenBys.Clear();
					}

					return Expression.Call(null, MethodInfoFastRef.QueryableExtensionsOrderByThenBysHelperMethod.MakeGenericMethod(methodCallExpression.Method.ReturnType.GetGenericArguments()[0]), this.Visit(methodCallExpression.Arguments[0]), Expression.Constant(orderByThenBys.ToArray()), Expression.Constant(sortBys.ToArray()));
				}
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
