using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlJoinConditionExpander
		: SqlExpressionVisitor
	{
		private bool inJoinCondition;

		private SqlJoinConditionExpander()
		{
		}

		public static Expression Expand(Expression expression)
		{
			return new SqlJoinConditionExpander().Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var inJoinConditionSave = this.inJoinCondition;

			try
			{
				this.inJoinCondition = false;

				return base.VisitSelect(selectExpression);
			}
			finally
			{
				this.inJoinCondition = inJoinConditionSave;
			}
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (binaryExpression.NodeType != ExpressionType.Equal)
			{
				return binaryExpression;
			}

			if (!this.inJoinCondition)
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

			for (var i = 0; i < count; i++)
			{
				var current = Expression.Equal(this.Visit(left[i]), this.Visit(right[i]));

				retval = retval == null ? current : Expression.And(retval, current);
			}

			return retval;
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			Expression condition;

			var left = this.Visit(join.Left);
			var right = this.Visit(join.Right);

			var inJoinConditionSave = this.inJoinCondition;

			try
			{
				this.inJoinCondition = true;

				condition = this.Visit(join.JoinCondition);
			}
			finally
			{
				this.inJoinCondition = inJoinConditionSave;
			}

			if (left != join.Left || right != join.Right || condition != join.JoinCondition)
			{
				return new SqlJoinExpression(join.Type, join.JoinType, left, right, condition);
			}

			return join;
		}
	}
}
