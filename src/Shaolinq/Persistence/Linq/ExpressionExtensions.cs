// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform.Reflection;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public static class ExpressionExtensions
	{
		private static readonly ExpressionType[] expressionTypes = new[] { ExpressionType.Equal, ExpressionType.Constant, ExpressionType.AndAlso, ExpressionType.And, (ExpressionType)SqlExpressionType.ConstantPlaceholder, (ExpressionType)SqlExpressionType.Column };
		
		internal static IEnumerable<Expression> GetIncludeJoins(this Expression expression)
		{
			var select = expression as SqlSelectExpression;

			if (select != null)
			{
				yield return null;
			}

			var join = expression as SqlJoinExpression;

			if (join?.JoinType != SqlJoinType.Left)
			{
				yield break;
			}

			foreach (var value in join.Left.GetIncludeJoins())
			{
				if (value == null)
				{
					if (!SqlExpressionFinder.FindExists(join.JoinCondition, c => !expressionTypes.Contains(c.NodeType)))
					{
						if (SqlExpressionFinder.FindExists(join.JoinCondition, c => c.NodeType == ExpressionType.Equal))
						{
							yield return join.JoinCondition;
						}
					}
				}
				else
				{
					yield return value;
				}
			}

			foreach (var value in join.Right.GetIncludeJoins())
			{
				if (value == null)
				{
					if (!SqlExpressionFinder.FindExists(join.JoinCondition, c => !expressionTypes.Contains(c.NodeType)))
					{
						if (SqlExpressionFinder.FindExists(join.JoinCondition, c => c.NodeType == ExpressionType.Equal))
						{
							yield return join.JoinCondition;
						}
					}
				}
				else
				{
					yield return value;
				}
			}
		}

		internal static SqlSelectExpression GetLeftMostSelect(this Expression expression)
		{
			var select = expression as SqlSelectExpression;

			if (select != null)
			{
				return select;
			}

			var join = expression as SqlJoinExpression;

			if (join != null)
			{
				return GetLeftMostSelect(join.Left);
			}

			return null;
		}

		internal static Expression StripObjectBindingCalls(this Expression expression)
		{
			if (expression == null)
			{
				return null;
			}

			if (expression.NodeType == ExpressionType.Conditional)
			{
				var conditional = (ConditionalExpression)expression;

				if (conditional.IfTrue.NodeType == ExpressionType.Constant && ((ConstantExpression)conditional.IfTrue).Value == null)
				{
					return StripObjectBindingCalls(conditional.IfFalse);
				}
			}

			if (!expression.Type.IsDataAccessObjectType())
			{
				return expression;
			}

			return expression.Strip(c =>
			{
				if (c.NodeType != ExpressionType.Call)
				{
					return null;
				}

				var methodCallExpression = (MethodCallExpression)c;

				if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.DataAccessObjectExtensionsAddToCollectionMethod)
				{
					return StripObjectBindingCalls(methodCallExpression.Arguments[0]);
				}

				return null;
			});
		}

		internal static Expression StripForIncludeScanning(this Expression expression)
		{
			return expression?.Strip(c =>
			{
				if (c.NodeType != ExpressionType.Call)
				{
					return null;
				}

				var methodCallExpression = (MethodCallExpression)c;

				if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.QueryableExtensionsItemsMethod)
				{
					return methodCallExpression.Arguments[0].StripForIncludeScanning();
				}
				else if (methodCallExpression.Method.DeclaringType == typeof(Queryable))
				{
					return methodCallExpression.Arguments[0].StripForIncludeScanning();
				}

				return null;
			});
		}

		internal static Expression[] Split(this Expression expression, params ExpressionType[] binarySeparators)
		{
			var list = new List<Expression>();

			Split(expression, list, binarySeparators);

			return list.ToArray();
		}

		internal static Expression Join(this IEnumerable<Expression> list, ExpressionType binarySeparator)
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

		internal static LambdaExpression StripQuotes(this Expression expression)
		{
			while (expression.NodeType == ExpressionType.Quote)
			{
				expression = ((UnaryExpression)expression).Operand;
			}

			return (LambdaExpression)expression;
		}

		internal static Expression Strip(this Expression expression, Func<Expression, Expression> inner)
		{
			Expression result;

			expression.TryStrip(inner, out result);

			return result;
		}

		internal static Expression StripConstantWrappers(this Expression expression)
		{
			return expression.Strip(c =>
			{
				if (c.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)
				{
					return ((SqlConstantPlaceholderExpression)expression).ConstantExpression;
				}

				if (c.CanReduce)
				{
					var reduced = c.Reduce();

					if (reduced.NodeType == ExpressionType.Constant)
					{
						return reduced;
					}
					else
					{
						return reduced.StripConstantWrappers();
					}
				}

				return null;
			});
		}

		internal static ConstantExpression StripAndGetConstant(this Expression expression)
		{
			return expression.Strip(c =>
			{
				if (c.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)
				{
					return ((SqlConstantPlaceholderExpression)expression).ConstantExpression;
				}

				if (c.CanReduce)
				{
					var reduced = c.Reduce();

					if (reduced.NodeType == ExpressionType.Constant)
					{
						return reduced;
					}
					else if (reduced.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)
					{
						return ((SqlConstantPlaceholderExpression)expression).ConstantExpression;
					}
					else
					{
						return reduced.StripAndGetConstant();
					}
				}

				return null;
			}) as ConstantExpression;
		}

		internal static bool TryStripDistinctCall(this Expression expression, out Expression result)
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

		internal static bool TryStripDefaultIfEmptyCall(this Expression expression, out Expression result, out Expression defaultIfEmptyValue, bool methodWithSelectorOnly = false)
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

		internal static bool TryStripDefaultIfEmptyCalls(this Expression expression, out Expression retval)
		{
			return expression.TryStrip(c => c.NodeType == ExpressionType.Call
										&& (((c as MethodCallExpression)?.Method.IsGenericMethod ?? false)
										&& (((MethodCallExpression)c).Method.GetGenericMethodDefinition() == MethodInfoFastRef.QueryableDefaultIfEmptyMethod) 
											|| ((MethodCallExpression)c).Method.GetGenericMethodDefinition() == MethodInfoFastRef.EnumerableDefaultIfEmptyMethod)
											? ((MethodCallExpression)c).Arguments[0] : null,
										out retval);
		}

		internal static bool TryStrip(this Expression expression, Func<Expression, Expression> inner, out Expression retval)
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
