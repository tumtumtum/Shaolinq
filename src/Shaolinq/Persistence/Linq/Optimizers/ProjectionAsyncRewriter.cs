using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Platform;
using ExpressionVisitor = Platform.Linq.ExpressionVisitor;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class ProjectionAsyncRewriter
		: ExpressionVisitor
	{
		private readonly ParameterExpression cancellationToken;

		private ProjectionAsyncRewriter(ParameterExpression cancellationToken)
		{
			this.cancellationToken = cancellationToken;
		}

		public static Expression Rewrite(Expression expression, ParameterExpression cancellationToken)
		{
			return new ProjectionAsyncRewriter(cancellationToken).Visit(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.DeclaringType == typeof(Enumerable) && methodCallExpression.Method.IsGenericMethod)
			{
				var genericArgs = methodCallExpression.Method.GetGenericArguments();

				if (genericArgs.Length == 1)
				{
					var name = methodCallExpression.Method.Name + "Async";
					var paramTypes = methodCallExpression.Method.GetParameters().Select(c => c.ParameterType).Concat(typeof(CancellationToken));
					var asyncMethods = typeof(EnumerableExtensions)
						.GetMethods()
						.Where(c => c.Name == name)
						.Where(c => c.IsGenericMethod)
						.Where(c => c.GetGenericArguments().Length == 1)
						.Select(c => c.MakeGenericMethod(genericArgs[0]))
						.ToList();

					var asyncMethod = asyncMethods
						.SingleOrDefault(c => c.GetParameters().Select(d => d.ParameterType).SequenceEqual(paramTypes));

					if (asyncMethod != null)
					{
						var parameters = this.VisitExpressionList(methodCallExpression.Arguments);

						return Expression.Call(asyncMethod, parameters.Concat(cancellationToken).ToArray());
					}

					paramTypes = methodCallExpression.Method.GetParameters().Select(c => c.ParameterType);

					asyncMethod = asyncMethods
						.SingleOrDefault(c => c.GetParameters().Select(d => d.ParameterType).SequenceEqual(paramTypes));

					if (asyncMethod != null)
					{
						var parameters = this.VisitExpressionList(methodCallExpression.Arguments);

						return Expression.Call(asyncMethod, parameters.ToArray());
					}
				}
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
