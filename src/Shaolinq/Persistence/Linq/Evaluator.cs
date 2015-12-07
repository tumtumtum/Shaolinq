// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public static class Evaluator
	{
		/// <summary>
		/// Performs evaluation & replacement of independent sub-trees
		/// </summary>
		/// <param name="expression">The root of the expression tree.</param>
		/// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns>
		public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
		{
			return SubtreeEvaluator.Eval(new EvaluatorNominator(fnCanBeEvaluated).Nominate(expression), expression);
		}

		/// <summary>
		/// Performs evaluation & replacement of independent sub-trees
		/// </summary>
		/// <param name="expression">The root of the expression tree.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns>
		public static Expression PartialEval(Expression expression)
		{
			return PartialEval(expression, CanBeEvaluatedLocally);
		}

		internal static bool CanBeEvaluatedLocally(Expression expression)
		{
			if (expression.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)
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

			internal static Expression Eval(HashSet<Expression> candidates, Expression expression)
			{
				var evalutator = new SubtreeEvaluator(candidates);

				return evalutator.Visit(expression);
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
					return e;
				}
				else if (e.NodeType == ExpressionType.Convert && ((UnaryExpression)e).Operand.NodeType == ExpressionType.Constant)
				{
					var unaryExpression = (UnaryExpression)e;
					var constantValue = ((ConstantExpression)(((UnaryExpression)e).Operand)).Value;

					if (constantValue == null)
					{
						return Expression.Constant(null, e.Type);
					}

					if (unaryExpression.Type.IsNullableType())
					{
						return Expression.Constant(Convert.ChangeType(constantValue, Nullable.GetUnderlyingType(unaryExpression.Type)), e.Type);
					}
					else
					{
						return Expression.Constant(Convert.ChangeType(constantValue, unaryExpression.Type), e.Type);
					}
				}
				else if (e.NodeType == ExpressionType.Convert 
                    && ((UnaryExpression)e).Operand.Type.GetUnwrappedNullableType().IsEnum)
				{
					value = ExpressionInterpreter.Interpret(e);

					return Expression.Constant(value, e.Type);
				}
				else if (e.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)
				{
					return e;
				}
				else
				{
					value = ExpressionInterpreter.Interpret(e);

					return new SqlConstantPlaceholderExpression(this.index++, Expression.Constant(value, e.Type));
				}
			}
		}

		/// <summary>
		/// Performs bottom-up analysis to determine which nodes can possibly be part of an evaluated sub-tree.
		/// </summary>
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
