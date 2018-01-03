// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
				return false;
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


			if (expression is MemberExpression memberExpression)
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


			if (expression is MethodCallExpression callExpression)
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
		/// Evaluates and replaces sub-trees when first candidate is reached (top-down)
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
				if (e.NodeType == ExpressionType.Constant)
				{
					return e.Type.IsPrimitive || ((ConstantExpression)e).Value == null ? e : new SqlConstantPlaceholderExpression(this.index++, (ConstantExpression)e);
				}
				
				if (e.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)
				{
					return e;
				}

				var value = ExpressionInterpreter.Interpret(e);
				
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
