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
					var methodName = "";
					Expression arg1;

					switch (methodCallExpression.Method.Name)
					{
					case "First":
					case "FirstOrDefault":
					case "Single":
					case "SingleOrDefault":
					case "Count":
					case "Any":
						methodName = methodCallExpression.Method.Name;
						arg1 = methodCallExpression.Arguments[1];
						break;
					default:
						if (methodCallExpression.Method.Name == "Contains" && methodCallExpression.Arguments[1].Type.IsDataAccessObjectType())
						{
							methodName = "Any";
							var param = Expression.Parameter(methodCallExpression.Arguments[1].Type);

							var body = Expression.Equal(param, methodCallExpression.Arguments[1]);

							arg1 = Expression.Lambda(body, param);

							break;
						}
						return base.VisitMethodCall(methodCallExpression);
					}
					
					var type = methodCallExpression.Method.GetGenericArguments()[0];
					var call = (Expression)Expression.Call(null, MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(type), methodCallExpression.Arguments[0], arg1);

					var method = methodCallExpression.Method.ReflectedType
						.GetMethods()
						.Single(c => c.Name == methodName && c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(IQueryable<>).MakeGenericType(c.GetGenericArguments()[0]));
						
					call = Expression.Call(null, method.MakeGenericMethod(type), call);

					return call;
				}
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
