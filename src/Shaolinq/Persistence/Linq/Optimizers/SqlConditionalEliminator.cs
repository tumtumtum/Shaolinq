// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlConditionalEliminator
		: SqlExpressionVisitor
	{
		private SqlConditionalEliminator()
		{
		}

		public static Expression Eliminate(Expression expression)
		{
			return new SqlConditionalEliminator().Visit(expression);
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{

			if (expression.Test is ConstantExpression constantExpression)
			{
				return Convert.ToBoolean(constantExpression.Value) ? expression.IfTrue : expression.IfFalse;
			}

			return base.VisitConditional(expression);
		}
	}
}
