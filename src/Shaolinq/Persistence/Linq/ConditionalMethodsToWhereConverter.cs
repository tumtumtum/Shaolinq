using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ConditionalMethodsToWhereConverter
		: SqlExpressionVisitor
	{
		public static Expression Convert(Expression expression)
		{
			return new ConditionalMethodsToWhereConverter().Visit(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.DeclaringType == typeof(Queryable))
			{
				if (methodCallExpression.Method.IsGenericMethod
					&& methodCallExpression.Arguments.Count == 2)
				{
					switch (methodCallExpression.Method.Name)
					{
					case "First":
					case "FirstOrDefault":
					case "Single":
					case "SingleOrDefault":
					case "Count":
						break;
					default:
						return base.VisitMethodCall(methodCallExpression);
					}
					
					var type = methodCallExpression.Method.GetGenericArguments()[0];
					var call = (Expression)Expression.Call(null, MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(type), methodCallExpression.Arguments[0], methodCallExpression.Arguments[1]);

					var method = methodCallExpression.Method.ReflectedType
						.GetMethods()
						.Single(c => c.Name == methodCallExpression.Method.Name && c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(IQueryable<>).MakeGenericType(c.GetGenericArguments()[0]));
						
					call = Expression.Call(null, method.MakeGenericMethod(type), call);

					return call;
				}
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
