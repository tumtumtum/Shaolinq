// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
		public static Expression PartialEval(DataAccessModel dataAccessModel, Expression expression, Func<Expression, bool> fnCanBeEvaluated)
		{
			return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(dataAccessModel, expression);
		}

		/// <summary>
		/// Performs evaluation & replacement of independent sub-trees
		/// </summary>
		/// <param name="expression">The root of the expression tree.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns>
		public static Expression PartialEval(DataAccessModel dataAccessModel, Expression expression)
		{
			return PartialEval(dataAccessModel, expression, CanBeEvaluatedLocally);
		}

		internal static bool CanBeEvaluatedLocally(Expression expression)
		{
			if (!(expression.NodeType != ExpressionType.Parameter && (int)expression.NodeType < (int)SqlExpressionType.Table))
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

			if (expression is MethodCallExpression)
			{
				if (((MethodCallExpression)expression).Method.DeclaringType == typeof(Enumerable)
					|| ((MethodCallExpression)expression).Method.DeclaringType == typeof(Queryable))
				{
					if (((MethodCallExpression)expression).Method.Name != "DefaultIfEmpty"
						&& CanBeEvaluatedLocally(((MethodCallExpression)expression).Arguments[0]))
					{
						return true;
					}

					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Evaluates & replaces sub-trees when first candidate is reached (top-down)
		/// </summary>
		private class SubtreeEvaluator : SqlExpressionVisitor
		{
			private int index;
			private DataAccessModel dataAccessModel;
			private readonly HashSet<Expression> candidates;

			internal SubtreeEvaluator(HashSet<Expression> candidates)
			{
				this.candidates = candidates;
			}

			internal Expression Eval(DataAccessModel dataAccessModel, Expression exp)
			{
				this.dataAccessModel = dataAccessModel;

				return Visit(exp);
			}

			protected override Expression Visit(Expression expression)
			{
				if (expression == null)
				{
					return null;
				}

				if (candidates.Contains(expression))
				{
					return Evaluate(expression);
				}

				return base.Visit(expression);
			}

			private Expression Evaluate(Expression e)
			{
				object value;

				if (e.NodeType == ExpressionType.Constant)
				{
					value = ((ConstantExpression)e).Value;

					return Expression.Constant(value, e.Type);
				}
				else if (e.NodeType == ExpressionType.Convert && ((UnaryExpression)e).Operand.NodeType == ExpressionType.Constant)
				{
					var unaryExpression = (UnaryExpression)e;
					var constantValue = ((ConstantExpression)(((UnaryExpression)e).Operand)).Value;

					if (constantValue == null)
					{
						return Expression.Constant(null, e.Type);
					}

					if (Nullable.GetUnderlyingType(unaryExpression.Type) != null)
					{
						return Expression.Constant(Convert.ChangeType(constantValue, Nullable.GetUnderlyingType(unaryExpression.Type)), e.Type);
					}
					else
					{
						return Expression.Constant(Convert.ChangeType(constantValue, unaryExpression.Type), e.Type);
					}

					// return Expression.Constant(value, e.Type);
				}
				else if (e.NodeType == ExpressionType.Convert && ((UnaryExpression)e).Operand.NodeType == ExpressionType.MemberAccess && ((UnaryExpression)e).Operand.Type.IsEnum)
				{
					var lambda = Expression.Lambda(e);
					var fn = lambda.Compile();

					value = fn.DynamicInvoke(null);

					return Expression.Constant(value, e.Type);
				}
				else
				{
					var lambda = Expression.Lambda(e);
					var fn = lambda.Compile();

					value = fn.DynamicInvoke(null);

					return new SqlConstantPlaceholderExpression(this.index++, Expression.Constant(value, e.Type));
				}
			}
		}

		/// <summary>
		/// Performs bottom-up analysis to determine which nodes can possibly
		/// be part of an evaluated sub-tree.
		/// </summary>
		internal class Nominator : SqlExpressionVisitor
		{
			private bool cannotBeEvaluated;
			private HashSet<Expression> candidates;
			private readonly Func<Expression, bool> fnCanBeEvaluated;

			private Expression first;

			internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
			{
				this.fnCanBeEvaluated = fnCanBeEvaluated;
			}

			internal HashSet<Expression> Nominate(Expression expression)
			{
				candidates = new HashSet<Expression>();

				first = expression;

				Visit(expression);

				return candidates;
			}

			protected override Expression Visit(Expression expression)
			{
				if (expression != null)
				{
					bool saveCannotBeEvaluated = cannotBeEvaluated;

					cannotBeEvaluated = false;

					base.Visit(expression);

					if (!cannotBeEvaluated)
					{
						if (expression != first && fnCanBeEvaluated(expression))
						{
							candidates.Add(expression);
						}
						else
						{
							cannotBeEvaluated = true;
						}
					}

					cannotBeEvaluated |= saveCannotBeEvaluated;
				}

				return expression;
			}
		}
	}
}
