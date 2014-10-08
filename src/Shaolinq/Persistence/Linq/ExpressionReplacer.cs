// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	/// <summary>
	/// Replaces an expression within an expression tree with another expression
	/// </summary>
	public class ExpressionReplacer
		: SqlExpressionVisitor
	{
		private readonly Func<Expression, Expression> selector;
		
		private ExpressionReplacer(Func<Expression, Expression> selector)
		{
			this.selector = selector;
		}

		private ExpressionReplacer(Expression searchFor, Expression replaceWith, Comparison<Expression> compareExpressions)
		{
			if (compareExpressions != null)
			{
				this.selector = c => compareExpressions(c, searchFor) == 0 ? replaceWith : null;
			}
			else
			{
				this.selector = c => c == searchFor ? replaceWith : null;
			}
		}

		public static Expression Replace(Expression expression, Func<Expression, Expression> selector)
		{
			return new ExpressionReplacer(selector).Visit(expression);
		}

		/// <summary>
		/// Walks an <see cref="expression"/>; finds the <see cref="searchFor"/> expression
		/// and replaces it with <see cref="replaceWith"/>.  Uses an object identity
		/// comparison to identify if <see cref="searchFor"/> and <see cref="replaceWith"/>
		/// are the same.
		/// </summary>
		/// <param name="expression">The expression to look withib</param>
		/// <param name="searchFor">The expression to look for</param>
		/// <param name="replaceWith">The expression to replace with</param>
		/// <returns>
		/// The original expression with the <see cref="searchFor"/> replaced 
		/// by <see cref="replaceWith"/> if <see cref="searchFor"/> was found
		/// </returns>
		public static Expression Replace(Expression expression, Expression searchFor, Expression replaceWith)
		{
			return Replace(expression, searchFor, replaceWith, null);
		}

		/// <summary>
		/// Walks an <see cref="expression"/>; finds the <see cref="searchFor"/> expression
		/// and replaces it with <see cref="replaceWith"/>.  Uses the provided
		/// <see cref="Comparison{OBJECT_TYPE}"/> to compare <see cref="searchFor"/> and <see cref="replaceWith"/>.
		/// </summary>
		/// <param name="expression">The expression to look withib</param>
		/// <param name="searchFor">The expression to look for</param>
		/// <param name="replaceWith">The expression to replace with</param>
		/// <param name="compareExpressions">A <see cref="Comparison{OBJECT_TYPE}"/> 
		/// used to compare <see cref="searchFor"/> and <see cref="replaceWith"/></param>
		/// <returns>
		/// The original expression with the <see cref="searchFor"/> replaced 
		/// by <see cref="replaceWith"/> if <see cref="searchFor"/> was found
		/// </returns>
		public static Expression Replace(Expression expression, Expression searchFor, Expression replaceWith, Comparison<Expression> compareExpressions)
		{
			return new ExpressionReplacer(searchFor, replaceWith, compareExpressions).Visit(expression);
		}

		protected override Expression Visit(Expression expression)
		{
			if (expression == null)
			{
				return null;
			}

			return selector(expression) ?? base.Visit(expression);
		}
	}
}
