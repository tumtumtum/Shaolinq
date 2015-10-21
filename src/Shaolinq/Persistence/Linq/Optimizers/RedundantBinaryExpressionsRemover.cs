// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class RedundantBinaryExpressionsRemover
		: SqlExpressionVisitor
	{
		public static Expression Remove(Expression expression)
		{
			return new RedundantBinaryExpressionsRemover().Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var from = selectExpression.From;
			var where = Visit(selectExpression.Where);

			var orderBy = selectExpression.OrderBy;
			var groupBy = selectExpression.GroupBy;
			var skip = selectExpression.Skip;
			var take = selectExpression.Take;
			var columns = selectExpression.Columns;

			if (where?.NodeType == ExpressionType.Constant && where.Type == typeof(bool))
			{
				var value = (bool)((ConstantExpression)where).Value;

				if (value)
				{
					where = null;
				}
			}

			if (where != selectExpression.Where)
			{
				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, columns, from, where, orderBy, groupBy, selectExpression.Distinct, skip, take, selectExpression.ForUpdate);
			}

			return base.VisitSelect(selectExpression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (binaryExpression.Left.NodeType == ExpressionType.Constant && binaryExpression.Left.Type == typeof(bool))
			{
				var leftValue = (bool)((ConstantExpression)binaryExpression.Left).Value;

				switch (binaryExpression.NodeType)
				{
					case ExpressionType.AndAlso:
						if (leftValue)
						{
							return binaryExpression.Right;
						}
						else
						{
							return Expression.Constant(false);
						}
					case ExpressionType.OrElse:
						if (leftValue)
						{
							return Expression.Constant(true);
						}
						else
						{
							return binaryExpression.Right;
						}
				}
			}
			else if (binaryExpression.Right.NodeType == ExpressionType.Constant
				&& binaryExpression.Right.Type == typeof(bool))
			{
				var rightValue = (bool)((ConstantExpression)binaryExpression.Right).Value;

				switch (binaryExpression.NodeType)
				{
					case ExpressionType.AndAlso:
						if (rightValue)
						{
							return binaryExpression.Left;
						}
						else
						{
							return Expression.Constant(false);
						}
					case ExpressionType.OrElse:
						if (rightValue)
						{
							return Expression.Constant(true);
						}
						else
						{
							return binaryExpression.Left;
						}
				}
			}

			return base.VisitBinary(binaryExpression);
		}
	}
}
