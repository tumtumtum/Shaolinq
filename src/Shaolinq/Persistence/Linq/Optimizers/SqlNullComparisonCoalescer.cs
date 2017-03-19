// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlNullComparisonCoalescer
		: SqlExpressionVisitor
	{
		private readonly Expression ignoreExpression;

		private SqlNullComparisonCoalescer(SqlProjectionExpression rootProjection)
		{
			this.ignoreExpression = rootProjection.Projector;
		}

		public static Expression Coalesce(Expression expression)
		{
			return new SqlNullComparisonCoalescer(expression as SqlProjectionExpression).Visit(expression);
		}

		protected override Expression Visit(Expression expression)
		{
			if (this.ignoreExpression == expression)
			{
				return expression;
			}

			return base.Visit(expression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			var nodeType = binaryExpression.NodeType;

			if (nodeType == ExpressionType.Equal || nodeType == ExpressionType.NotEqual)
			{
				var left = this.Visit(binaryExpression.Left).StripAndGetConstant();
				var right = this.Visit(binaryExpression.Right).StripAndGetConstant();

				if (left != null && right != null)
				{
					if (left.Value == null && right.Value == null)
					{
						return Expression.Constant(true);
					}

					if (left.Value == null || right.Value == null)
					{
						return Expression.Constant(false);
					}
				}

				if (left != null && left.Value == null)
				{
					return new SqlFunctionCallExpression(binaryExpression.Type, nodeType == ExpressionType.Equal ? SqlFunction.IsNull : SqlFunction.IsNotNull, binaryExpression.Right);
				}
				else if (right != null && right.Value == null)
				{
					return new SqlFunctionCallExpression(binaryExpression.Type, nodeType == ExpressionType.Equal ? SqlFunction.IsNull : SqlFunction.IsNotNull, binaryExpression.Left);
				}
			}

			return base.VisitBinary(binaryExpression);
		}
	}
}
