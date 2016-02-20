// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Shaolinq.TypeBuilding;
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
		
		private bool IsTaskType(Type type)
		{
			return type.GetGenericTypeDefinitionOrNull() == typeof(Task<>) || type == typeof(Task);
		}

		internal static Expression CreateChainedCalls(params ParameterExpression[] expressions)
		{
			Expression body = null;

			// e1.ContinueWith(c => e2).Unwrap().ContinueWith(c => e3).Unwrap() ...

			foreach (var expression in expressions)
			{
				if (body == null)
				{
					body = expression;

					continue;
				}

				var lambda = Expression.Lambda(expression, Expression.Parameter(body.Type));
				var method = MethodInfoFastRef.TaskExtensionsUnwrapMethod.MakeGenericMethod(body.Type.GetGenericArguments()[0]);

				body = Expression.Call(method, Expression.Call(body, "ContinueWith", new[] { expression.Type }, lambda));
			}

			return body;
		}
		
		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			var left = this.Visit(binaryExpression.Left);
			var right = this.Visit(binaryExpression.Right);

			if (left.Type == binaryExpression.Left.Type && right.Type == binaryExpression.Right.Type)
			{
				return binaryExpression.ChangeLeftRight(left, right);
			}

			if (!IsTaskType(left.Type) && !IsTaskType(right.Type))
			{
				return binaryExpression.ChangeLeftRight(left, right);
			}

			if (IsTaskType(left.Type) && IsTaskType(right.Type))
			{
				var leftVar = Expression.Parameter(left.Type);
				var rightVar = Expression.Parameter(right.Type);
				var variables = new[] { leftVar, rightVar };

				var expression = CreateChainedCalls(leftVar, rightVar);
				var lambda = Expression.Lambda(binaryExpression.ChangeLeftRight(Expression.Property(leftVar, "Result"), Expression.Property(rightVar, "Result")), Expression.Parameter(expression.Type));

				return Expression.Block(variables, Expression.Assign(leftVar, left), Expression.Assign(rightVar, right), Expression.Call(expression, "ContinueWith", new[] { lambda.ReturnType }, lambda));
			}

			if (this.IsTaskType(left.Type))
			{
				var leftVar = Expression.Parameter(left.Type);
				var variables = new[] { leftVar };

				var expression = CreateChainedCalls(leftVar);
				var lambda = Expression.Lambda(binaryExpression.ChangeLeftRight(Expression.Property(leftVar, "Result"), right), Expression.Parameter(expression.Type));

				return Expression.Block(variables, Expression.Assign(leftVar, left), Expression.Call(expression, "ContinueWith", new[] { lambda.ReturnType }, lambda));
			}

			if (this.IsTaskType(right.Type))
			{
				var rightVar = Expression.Parameter(right.Type);
				var variables = new[] { rightVar };

				var expression = CreateChainedCalls(rightVar);
				var lambda = Expression.Lambda(binaryExpression.ChangeLeftRight(left, Expression.Property(rightVar, "Result")), Expression.Parameter(expression.Type));

				return Expression.Block(variables,  Expression.Assign(rightVar, right), Expression.Call(expression, "ContinueWith", new[] { lambda.ReturnType }, lambda));
			}

			return base.VisitBinary(binaryExpression);
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			if (unaryExpression.NodeType == ExpressionType.Convert)
			{
				var operand = this.Visit(unaryExpression.Operand);

				if (operand.Type != unaryExpression.Operand.Type && IsTaskType(operand.Type))
				{
					var param = Expression.Parameter(operand.Type);
					var result = Expression.Property(Expression.Convert(param, operand.Type), "Result");

					var lambda = Expression.Lambda(Expression.Convert(result, unaryExpression.Type), param);

					return Expression.Call(operand, "ContinueWith", new[] { lambda.ReturnType }, lambda);
				}
			}

			return base.VisitUnary(unaryExpression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if ((methodCallExpression.Method.DeclaringType == typeof(Enumerable) || methodCallExpression.Method.DeclaringType == typeof(EnumerableExtensions))
				&& methodCallExpression.Method.IsGenericMethod)
			{
				var genericArgs = methodCallExpression.Method.GetGenericArguments();

				if (genericArgs.Length == 1)
				{
					var methodParameters = methodCallExpression.Method.GetParameters();
					var name = methodCallExpression.Method.Name + "Async";
					var paramTypes = methodParameters.Select(c => c.ParameterType).Concat(typeof(CancellationToken));
					var asyncMethods = typeof(EnumerableExtensions)
						.GetMethods()
						.Where(c => c.Name == name)
						.Where(c => c.IsGenericMethod)
						.Where(c => c.GetGenericArguments().Length == 1)
						.Select(c => c.MakeGenericMethod(genericArgs[0]))
						.ToList();

					var asyncMethod = asyncMethods
						.SingleOrDefault(c => c.GetParameters().Select(d => d.ParameterType).SequenceEqual(paramTypes));

					List<Expression> parameters = null;

					if (asyncMethod != null)
					{
						parameters = this.VisitExpressionList(methodCallExpression.Arguments).Concat(cancellationToken).ToList();
					}
					else
					{
						paramTypes = methodCallExpression.Method.GetParameters().Select(c => c.ParameterType).ToList();

						asyncMethod = asyncMethods
							.SingleOrDefault(c => c.GetParameters().Select(d => d.ParameterType).SequenceEqual(paramTypes));

						if (asyncMethod != null)
						{
							parameters = this.VisitExpressionList(methodCallExpression.Arguments).ToList();
						}
					}

					List<Expression> newParams = null;
					List<ParameterExpression> taskParams = null;
						
					if (asyncMethod != null)
					{
						for (var i = 0; i < methodParameters.Length; i++)
						{
							if (parameters[i].Type != methodParameters[i].ParameterType)
							{
								if (IsTaskType(parameters[i].Type))
								{
									if (taskParams == null)
									{
										newParams = new List<Expression>();
										taskParams = new List<ParameterExpression>();
											
										for (var j = 0; j < i; j++)
										{
											newParams.Add(parameters[j]);
										}
									}

									var taskParam = Expression.Parameter(parameters[i].Type);
										
									taskParams.Add(taskParam);
									newParams.Add(Expression.Property(taskParam, "Result"));
								}
								else
								{
									newParams?.Add(parameters[i]);
								}
							}
						}

						if (taskParams == null)
						{
							return Expression.Call(asyncMethod, parameters.ToArray());
						}

						var chainedCalls = CreateChainedCalls(taskParams.ToArray());

						var lambda = Expression.Lambda(Expression.Call(asyncMethod, newParams.ToArray()), Expression.Parameter(chainedCalls.Type));
						var call = Expression.Call(MethodInfoFastRef.TaskExtensionsUnwrapMethod, Expression.Call(chainedCalls, "ContinueWith", new[] { asyncMethod.ReturnType }, lambda));

						var block = Expression.Block
						(
							taskParams,
							call
						);
							
						return block;
					}
				}
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
