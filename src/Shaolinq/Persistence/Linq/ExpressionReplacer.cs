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
		private readonly Expression searchFor;
		private readonly Expression replaceWith;
		private readonly Comparison<Expression> compareExpressions;
		
		private ExpressionReplacer(Expression searchFor, Expression replaceWith, Comparison<Expression> compareExpressions)
		{
			this.searchFor = searchFor;
			this.replaceWith = replaceWith;
			this.compareExpressions = compareExpressions;
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
			if (this.compareExpressions != null)
			{
				if (this.compareExpressions(expression, this.searchFor) == 0)
				{
					return this.replaceWith;
				}

				return base.Visit(expression);
			}

			if (expression == this.searchFor)
			{
				return this.replaceWith;
			}

			return base.Visit(expression);
		}
	}
}
