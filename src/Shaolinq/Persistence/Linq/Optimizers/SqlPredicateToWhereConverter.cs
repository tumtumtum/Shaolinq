// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlPredicateToWhereConverter
		: SqlExpressionVisitor
	{
		public static Expression Convert(Expression expression)
		{
			return new SqlPredicateToWhereConverter().Visit(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.DeclaringType == typeof(Queryable) || methodCallExpression.Method.DeclaringType == typeof(QueryableExtensions))
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
					case "Delete":
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
					var call = (Expression)Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(type), methodCallExpression.Arguments[0], arg1);

					var method = methodCallExpression
						.Method
						.ReflectedType
						.GetMethods()
						.Single(c => c.Name == methodName && c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(IQueryable<>).MakeGenericType(c.GetGenericArguments()[0]));
						
					call = Expression.Call(method.MakeGenericMethod(type), call);

					return call;
				}
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
