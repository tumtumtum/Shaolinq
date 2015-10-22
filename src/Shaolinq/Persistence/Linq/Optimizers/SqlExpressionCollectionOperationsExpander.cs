// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlExpressionCollectionOperationsExpander
		: SqlExpressionVisitor
	{
		private SqlExpressionCollectionOperationsExpander()
		{
		}

		public static Expression Expand(Expression expression)
		{
			var visitor = new SqlExpressionCollectionOperationsExpander();

			return visitor.Visit(expression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (binaryExpression.NodeType == ExpressionType.NotEqual
				|| binaryExpression.NodeType == ExpressionType.Equal)
			{
				var function = binaryExpression.NodeType == ExpressionType.NotEqual ? SqlFunction.IsNotNull : SqlFunction.IsNull;

				var functionCallExpression = binaryExpression.Left as SqlFunctionCallExpression;
				var otherExpression = binaryExpression.Right;
				
				if (functionCallExpression == null)
				{
					functionCallExpression = binaryExpression.Right as SqlFunctionCallExpression;
					otherExpression = binaryExpression.Left;
				}

				if (functionCallExpression != null && functionCallExpression.Function == SqlFunction.CollectionCount)
				{
					var constantExpression = otherExpression as ConstantExpression;

					if (constantExpression != null)
					{
						if (constantExpression.Type == typeof(int) || constantExpression.Type == typeof(long))
						{
							if (Convert.ToInt32(constantExpression.Value) == 0)
							{
								var isNull = new SqlFunctionCallExpression(typeof(bool) ,SqlFunction.IsNull ,functionCallExpression.Arguments[0]);
								var isEmpty = Expression.Equal(functionCallExpression.Arguments[0] ,Expression.Constant(""));

								return Expression.Or(isNull ,isEmpty);
							}
						}
					}

					throw new NotSupportedException("Blobbed list counts can only be compared to const 0");
				}
			}
			return base.VisitBinary(binaryExpression);
		}
	}
}
