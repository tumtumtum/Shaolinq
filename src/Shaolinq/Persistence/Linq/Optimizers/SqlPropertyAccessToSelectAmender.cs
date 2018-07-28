// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Converts property accesses off single objects into selects.
	/// </summary>
	/// <remarks>
	/// <c>Where(c => c.Animals.First().Name == "")</c> gets converted to <c>Where(c => c.Animals.Select(d => .Name).First() == "")</c> 
	/// </remarks>
	public class SqlPropertyAccessToSelectAmender
		: SqlExpressionVisitor
	{
		public static Expression Amend(Expression expression)
		{
			return new SqlPropertyAccessToSelectAmender().Visit(expression);
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			var expression = Visit(memberExpression.Expression);

			if (expression is MethodCallExpression methodCall 
				&& methodCall.Method.IsGenericMethod
				&& (methodCall.Method.DeclaringType == typeof(Queryable) || methodCall.Method.DeclaringType == typeof(Enumerable)))
			{
				if (methodCall.Method.Name == nameof(Queryable.First)|| methodCall.Method.Name == nameof(Queryable.FirstOrDefault)
					|| methodCall.Method.Name == nameof(Queryable.Single) || methodCall.Method.Name == nameof(Queryable.SingleOrDefault))
				{
					var type = methodCall.Method.ReturnType;
					var param = Expression.Parameter(methodCall.Method.ReturnType);
					var body = Expression.MakeMemberAccess(param, memberExpression.Member);
					var lambda = Expression.Lambda(body, param);

					var select = Expression.Call(MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod(type, lambda.ReturnType), Visit(methodCall.Arguments[0]), Expression.Quote(lambda));

					var retval = Expression.Call(methodCall.Method.GetGenericMethodDefinition().MakeGenericMethod(lambda.ReturnType), select);

					return retval;
				}
			}

			return base.VisitMemberAccess(memberExpression);
		}
	}
}
