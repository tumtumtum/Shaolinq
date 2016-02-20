﻿// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
		public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
		{
			return SubtreeEvaluator.Eval(new EvaluatorNominator(fnCanBeEvaluated).Nominate(expression), expression);
		}
		
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

			internal static Expression Eval(HashSet<Expression> candidates, Expression expression)
			{
				if (candidates.Count == 0)
				{
					return expression;
				}

				var evalutator = new SubtreeEvaluator(candidates) { index = SqlConstantPlaceholderMaxIndexFinder.Find(expression) };

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
					return e.Type.IsValueType || ((ConstantExpression)e).Value == null ? e : new SqlConstantPlaceholderExpression(this.index++, (ConstantExpression) e);
				}

				var unaryExpression = e as UnaryExpression;

				if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Convert)
				{
					if (unaryExpression.Operand.Type == e.Type || (unaryExpression.Type.IsAssignableFrom(unaryExpression.Operand.Type) && !(unaryExpression.Type.IsNullableType() || unaryExpression.Operand.Type.IsNullableType())))
					{
						return unaryExpression.Operand;
					}

					if ((unaryExpression.Operand.NodeType == ExpressionType.Constant || (unaryExpression.Operand.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)))
					{
						var constantValue = (unaryExpression.Operand as ConstantExpression)?.Value ?? (unaryExpression.Operand as SqlConstantPlaceholderExpression)?.ConstantExpression.Value;

						if (constantValue == null)
						{
							return Expression.Constant(null, unaryExpression.Type);
						}

						if (unaryExpression.Type.IsNullableType())
						{
							return Expression.Constant(Convert.ChangeType(constantValue, Nullable.GetUnderlyingType(unaryExpression.Type)), unaryExpression.Type);
						}

						if (unaryExpression.Type.IsInstanceOfType(constantValue))
						{
							var constantExpression = Expression.Constant(constantValue, unaryExpression.Type);

							return unaryExpression.Type.IsValueType ? constantExpression : (Expression)new SqlConstantPlaceholderExpression(this.index++, constantExpression);
						}

						if (typeof(IConvertible).IsAssignableFrom(unaryExpression.Type))
						{
							var constantExpression = Expression.Constant(Convert.ChangeType(constantValue, unaryExpression.Type));

							return unaryExpression.Type.IsValueType ? constantExpression : (Expression)new SqlConstantPlaceholderExpression(this.index++, constantExpression);
						}

						return unaryExpression;
					}
				}

				if (e.NodeType == ExpressionType.Convert && ((UnaryExpression)e).Operand.Type.GetUnwrappedNullableType().IsEnum)
				{
					value = ExpressionInterpreter.Interpret(e);

					return Expression.Constant(value, e.Type);
				}

				if (e.NodeType == (ExpressionType)SqlExpressionType.ConstantPlaceholder)
				{
					return e;
				}
				
				value = ExpressionInterpreter.Interpret(e);

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
