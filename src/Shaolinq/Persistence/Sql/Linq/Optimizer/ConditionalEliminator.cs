﻿using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	public class ConditionalEliminator
		: SqlExpressionVisitor
	{
		private ConditionalEliminator()
		{
		}

		public static Expression Eliminate(Expression expression)
		{
			return new ConditionalEliminator().Visit(expression);
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			var constantExpression = expression.Test as ConstantExpression;

			if (constantExpression != null)
			{
				if (Convert.ToBoolean(constantExpression.Value))
				{
					return expression.IfTrue; 
				}
				else
				{
					return expression.IfFalse;
				}
			}

			return base.VisitConditional(expression);
		}
	}
}