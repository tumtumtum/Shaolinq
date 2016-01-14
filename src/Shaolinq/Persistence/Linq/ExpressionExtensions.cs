// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Platform.Reflection;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public static class ExpressionExtensions
	{
		public static Expression RemovePlaceholderItem(this Expression expression)
		{
			var memberExpression = expression as MemberExpression;

			if (memberExpression == null)
			{
				return expression;
			}

			if (memberExpression.Member.DeclaringType == typeof(QueryableExtensions))
			{
				if (memberExpression.Member.Name == "Item")
				{
					return memberExpression.Expression;
				}
			}

			return expression;
		}

		public static Expression[] Split(this Expression expression, params ExpressionType[] binarySeparators)
		{
			var list = new List<Expression>();

			Split(expression, list, binarySeparators);

			return list.ToArray();
		}

		public static Expression Join(this IEnumerable<Expression> list, ExpressionType binarySeparator)
		{
			var array = list?.ToArray();
			
			return array?.Length > 0 ? array.Aggregate((x1, x2) => Expression.MakeBinary(binarySeparator, x1, x2)) : null;
		}

		private static void Split(Expression expression, ICollection<Expression> list, ExpressionType[] binarySeparators)
		{
			if (expression != null)
			{
				if (binarySeparators.Contains(expression.NodeType))
				{
					var binaryExpression = expression as BinaryExpression;

					if (binaryExpression != null)
					{
						Split(binaryExpression.Left, list, binarySeparators);
						Split(binaryExpression.Right, list, binarySeparators);
					}
				}
				else
				{
					list.Add(expression);
				}
			}
		}

		public static LambdaExpression StripQuotes(this Expression expression)
		{
			while (expression.NodeType == ExpressionType.Quote)
			{
				expression = ((UnaryExpression)expression).Operand;
			}

			return (LambdaExpression)expression;
		}

		public static Expression Strip(this Expression expression, Func<Expression, Expression> inner)
		{
			Expression result;

			expression.TryStrip(inner, out result);

			return result;
		}

		public static bool TryStripDistinctCall(this Expression expression, out Expression result)
		{
			if (expression.NodeType == ExpressionType.Call)
			{
				var methodCallExpression = expression as MethodCallExpression;

				if (methodCallExpression != null)
				{
					if (methodCallExpression.Method.Name == "Distinct"
						&& methodCallExpression.Arguments.Count == 1
						&& (methodCallExpression.Method.DeclaringType == typeof(Queryable) || methodCallExpression.Method.DeclaringType == typeof(Enumerable)))
					{
						result = methodCallExpression.Arguments[0];

						return true;
					}
				}
			}

			result = expression;

			return false;
		}

		public static bool TryStripDefaultIfEmptyCall(this Expression expression, out Expression result, out Expression defaultIfEmptyValue, bool methodWithSelectorOnly = false)
		{
			if (expression.NodeType == ExpressionType.Call)
			{
				var sourceMethodCall = expression as MethodCallExpression;

				if (sourceMethodCall == null)
				{
					defaultIfEmptyValue = null;
					result = expression;

					return false;
				}

				var method = sourceMethodCall.Method.GetGenericMethodOrRegular();

				if (method == MethodInfoFastRef.EnumerableDefaultIfEmptyMethod 
					|| method == MethodInfoFastRef.QueryableDefaultIfEmptyMethod
					|| method == MethodInfoFastRef.EnumerableDefaultIfEmptyWithValueMethod
					|| method == MethodInfoFastRef.QueryableDefaultIfEmptyWithValueMethod)
				{
					if (sourceMethodCall.Arguments.Count == 1 && methodWithSelectorOnly)
					{
						defaultIfEmptyValue = null;
						result = expression;

						return false;
					}

					result = sourceMethodCall.Arguments[0];
					defaultIfEmptyValue = sourceMethodCall.Arguments.Count == 2 ? sourceMethodCall.Arguments[1] : null;

					return true;
				}
			}

			defaultIfEmptyValue = null;
			result = expression;

			return false;
		}

		public static bool TryStripDefaultIfEmptyCalls(this Expression expression, out Expression retval)
		{
			return expression.TryStrip(c => c.NodeType == ExpressionType.Call
										&& (((c as MethodCallExpression)?.Method.IsGenericMethod ?? false)
										&& (((MethodCallExpression)c).Method.GetGenericMethodDefinition() == MethodInfoFastRef.QueryableDefaultIfEmptyMethod) 
											|| ((MethodCallExpression)c).Method.GetGenericMethodDefinition() == MethodInfoFastRef.EnumerableDefaultIfEmptyMethod)
											? ((MethodCallExpression)c).Arguments[0] : null,
										out retval);
		}

        public static bool TryStrip(this Expression expression, Func<Expression, Expression> inner, out Expression retval)
		{
			var didStrip = false;

            Expression current;
	        var previous = expression;

			while ((current = inner(previous)) != null)
			{
				previous = current;
				didStrip = true;
            }

			retval = previous;

			return didStrip;
		}
	}
}
