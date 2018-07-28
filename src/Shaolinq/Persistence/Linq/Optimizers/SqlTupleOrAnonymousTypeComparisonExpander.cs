// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlTupleOrAnonymousTypeComparisonExpander
		: SqlExpressionVisitor
	{
		private SqlTupleOrAnonymousTypeComparisonExpander()
		{
		}

		public static Expression Expand(Expression expression)
		{
			return new SqlTupleOrAnonymousTypeComparisonExpander().Visit(expression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (!(binaryExpression.NodeType == ExpressionType.Equal || binaryExpression.NodeType == ExpressionType.Equal))
			{
				return binaryExpression;
			}

			if (binaryExpression.Left.Type.IsDataAccessObjectType())
			{
				return binaryExpression;
			}

			List<Expression> left;
			List<Expression> right;

			if (binaryExpression.Left.NodeType == ExpressionType.MemberInit && binaryExpression.Right.NodeType == ExpressionType.MemberInit)
			{
				left = ((MemberInitExpression)binaryExpression.Left).Bindings.OfType<MemberAssignment>().Select(c => c.Expression).ToList();
				right = ((MemberInitExpression)binaryExpression.Right).Bindings.OfType<MemberAssignment>().Select(c => c.Expression).ToList();
			}
			else if (binaryExpression.Left.NodeType == ExpressionType.New && binaryExpression.Right.NodeType == ExpressionType.New)
			{
				left = ((NewExpression)binaryExpression.Left).Arguments.ToList();
				right = ((NewExpression)binaryExpression.Right).Arguments.ToList();
			}
			else
			{
				return binaryExpression;
			}

			var count = left.Count;
			Expression retval = null;

			if (count == 0 || count != right.Count)
			{
				return binaryExpression;
			}

			if (binaryExpression.NodeType == ExpressionType.Equal)
			{
				for (var i = 0; i < count; i++)
				{
					var current = Expression.Equal(Visit(left[i]), Visit(right[i]));

					retval = retval == null ? current : Expression.And(retval, current);
				}
			}
			else
			{
				for (var i = 0; i < count; i++)
				{
					var current = Expression.NotEqual(Visit(left[i]), Visit(right[i]));

					retval = retval == null ? current : Expression.Or(retval, current);
				}
			}
			
			return retval;
		}
	}
}
