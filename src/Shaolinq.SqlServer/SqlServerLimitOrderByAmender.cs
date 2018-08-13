using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	/// <summary>
	/// Add OrderBy to a select if it's got a skip/take but no order by expression.
	/// </summary>
	public class SqlServerLimitOrderByAmender
		: SqlExpressionVisitor
	{
		public static Expression Amend(Expression expression)
		{
			return new SqlServerLimitOrderByAmender().Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			if ((selectExpression.Skip != null || selectExpression.Take != null) && selectExpression.OrderBy == null)
			{
				var cols = selectExpression
					.Columns
					.Select(c => new SqlColumnDeclaration(c.Name, new SqlColumnExpression(c.Expression.Type, selectExpression.Alias, c.Name)))
					.ToList();

				selectExpression.ChangeOrderBy(cols.Select(c => new SqlOrderByExpression(OrderType.Ascending, c.Expression)));
			}

			return base.VisitSelect(selectExpression);
		}
	}
}