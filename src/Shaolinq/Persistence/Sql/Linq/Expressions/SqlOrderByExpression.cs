using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	/// <summary>
	/// A pairing of an expression and an order type for use in a SQL Order By clause
	/// </summary>
	public class SqlOrderByExpression
	{
		public OrderType OrderType { get; private set; }
		public Expression Expression { get; private set; }

		internal SqlOrderByExpression(OrderType orderType, Expression expression)
		{
			this.OrderType = orderType;
			this.Expression = expression;
		}
	}
}
