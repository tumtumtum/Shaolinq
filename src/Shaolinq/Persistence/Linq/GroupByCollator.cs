// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class GroupByCollator
		: SqlExpressionVisitor
	{
		private GroupByCollator()
		{
		}

		public static Expression Collate(Expression expression)
		{
			var visitor = new GroupByCollator();

			return visitor.Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			if (selectExpression.GroupBy != null && selectExpression.GroupBy.Count == 1
				&& selectExpression.GroupBy[0].NodeType == ExpressionType.New)
			{
				var groupBy = ((NewExpression)selectExpression.GroupBy[0]).Arguments;

				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, selectExpression.Columns.ToReadOnlyList(), selectExpression.From, selectExpression.Where, selectExpression.OrderBy.ToReadOnlyList(), groupBy.ToReadOnlyList(), selectExpression.Distinct, selectExpression.Skip, selectExpression.Take, selectExpression.ForUpdate);	
			}

			return base.VisitSelect(selectExpression);
		}
	}
}
