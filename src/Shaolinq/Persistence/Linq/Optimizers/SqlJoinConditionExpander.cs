using System.Linq.Expressions;
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
