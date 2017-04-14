// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public static class Evaluator
	{
		public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated, ref int placeholderCount)
		{
			return SubtreeEvaluator.Eval(new EvaluatorNominator(fnCanBeEvaluated).Nominate(expression), expression, ref placeholderCount);
		}
		
		public static Expression PartialEval(Expression expression)
		{
			var placeholderCount = -1;

			return PartialEval(expression, ref placeholderCount);
		}

		public static Expression PartialEval(Expression expression, ref int placeholderCount)
		{
			return PartialEval(expression, CanBeEvaluatedLocally, ref placeholderCount);
		}

		internal static bool CanBeEvaluatedLocally(Expression expression)
		{
			if (expression.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)
			{
				return true;	
			}

			if (expression.NodeType == ExpressionType.Constant)
			{
				return true;
			}

			if (((int)expression.NodeType >= (int)SqlExpressionType.Table))
			{
				return false;
			}

			if (expression.NodeType == ExpressionType.Parameter)
			{
				return false;
			}

			if ((expression as MethodCallExpression)?.Method == MethodInfoFastRef.TransactionContextGetCurrentContextVersion)
			{
				return false;
			}

			var memberExpression = expression as MemberExpression;

			if (memberExpression != null)
			{
				if (memberExpression.Member.DeclaringType == typeof(ServerDateTime))
				{
					return false;
				}
			}

			if (expression.NodeType == ExpressionType.Lambda)
			{
				return false;
			}

			var callExpression = expression as MethodCallExpression;

			if (callExpression != null)
			{
				var declaringType = callExpression.Method.DeclaringType;

				if (declaringType == typeof(Enumerable) || declaringType == typeof(Queryable) || declaringType == typeof(QueryableExtensions))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Evaluates & replaces sub-trees when first candidate is reached (top-down)
		/// </summary>
		private class SubtreeEvaluator
			: SqlExpressionVisitor
		{
			private int index;
			private readonly HashSet<Expression> candidates;

			private SubtreeEvaluator(HashSet<Expression> candidates)
			{
				this.candidates = candidates;
			}

			internal static Expression Eval(HashSet<Expression> candidates, Expression expression, ref int placeholderCount)
			{
				if (candidates.Count == 0)
				{
					return expression;
				}

				var i = placeholderCount >= 0 ? placeholderCount : SqlConstantPlaceholderMaxIndexFinder.Find(expression) + 1;

				var evaluator = new SubtreeEvaluator(candidates) { index = i };

				return evaluator.Visit(expression);
			}

			protected override Expression Visit(Expression expression)
			{
				if (expression == null)
				{
					return null;
				}

				if (this.candidates.Contains(expression))
				{
					return this.Evaluate(expression);
				}

				return base.Visit(expression);
			}
			
			private Expression Evaluate(Expression e)
			{
				object value;

				if (e.NodeType == ExpressionType.Constant)
				{
					return e.Type.IsPrimitive || ((ConstantExpression)e).Value == null ? e : new SqlConstantPlaceholderExpression(this.index++, (ConstantExpression) e);
				}

				var unaryExpression = e as UnaryExpression;

				if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Convert)
				{
					if (unaryExpression.Operand.Type == e.Type || (unaryExpression.Type.IsAssignableFrom(unaryExpression.Operand.Type) && !(unaryExpression.Type.IsNullableType() || unaryExpression.Operand.Type.IsNullableType())))
					{
						return this.Visit(unaryExpression.Operand);
					}

					if (unaryExpression.Operand.NodeType == ExpressionType.Constant || unaryExpression.Operand.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)
					{
						var constantValue = (unaryExpression.Operand as ConstantExpression)?.Value ?? (unaryExpression.Operand as SqlConstantPlaceholderExpression)?.ConstantExpression.Value;

						if (constantValue == null)
						{
							if (unaryExpression.Type.IsValueType && !unaryExpression.Type.IsNullableType())
							{
								throw new InvalidOperationException($"Unable to convert value null to type {unaryExpression.Type}");
							}

							return new SqlConstantPlaceholderExpression(this.index++, Expression.Constant(null, unaryExpression.Type));
						}

						if (unaryExpression.Type.IsNullableType())
						{
							return new SqlConstantPlaceholderExpression(this.index++, Expression.Constant(Convert.ChangeType(constantValue, Nullable.GetUnderlyingType(unaryExpression.Type)), unaryExpression.Type));
						}

						if (unaryExpression.Type.IsInstanceOfType(constantValue))
						{
							var constantExpression = Expression.Constant(constantValue, unaryExpression.Type);

							return unaryExpression.Type.IsValueType ? constantExpression : (Expression)new SqlConstantPlaceholderExpression(this.index++, constantExpression);
						}

						if (typeof(IConvertible).IsAssignableFrom(unaryExpression.Type))
						{
							var constantExpression = Expression.Constant(Convert.ChangeType(constantValue, unaryExpression.Type), unaryExpression.Type);

							return unaryExpression.Type.IsValueType ? constantExpression : (Expression)new SqlConstantPlaceholderExpression(this.index++, constantExpression);
						}

						return unaryExpression;
					}
				}

				if (e.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)
				{
					return e;
				}
				
				value = ExpressionInterpreter.Interpret(e);

				if (!e.Type.IsInstanceOfType(value))
				{
					throw new InvalidOperationException($"Unable to convert value {value} of type {value?.GetType().Name} to type {e.Type.Name}");
				}

				return new SqlConstantPlaceholderExpression(this.index++, Expression.Constant(value, e.Type));
			}
		}
		
		internal class EvaluatorNominator
			: SqlExpressionVisitor
		{
			private bool cannotBeEvaluated;
			private HashSet<Expression> candidates;
			private readonly Func<Expression, bool> fnCanBeEvaluated;

			private Expression first;

			internal EvaluatorNominator(Func<Expression, bool> fnCanBeEvaluated)
			{
				this.fnCanBeEvaluated = fnCanBeEvaluated;
			}

			internal HashSet<Expression> Nominate(Expression expression)
			{
				this.candidates = new HashSet<Expression>();

				this.first = expression;

				this.Visit(expression);

				return this.candidates;
			}

			protected override Expression Visit(Expression expression)
			{
				if (expression != null)
				{
					var saveCannotBeEvaluated = this.cannotBeEvaluated;

					this.cannotBeEvaluated = false;

					base.Visit(expression);

					if (!this.cannotBeEvaluated)
					{
						if (expression != this.first && this.fnCanBeEvaluated(expression))
						{
							this.candidates.Add(expression);
						}
						else
						{
							this.cannotBeEvaluated = true;
						}
					}

					this.cannotBeEvaluated |= saveCannotBeEvaluated;
				}

				return expression;
			}
		}
	}
}
