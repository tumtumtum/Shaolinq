// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlOrderByExpression
		: SqlBaseExpression
	{
		public OrderType OrderType { get; }
		public Expression Expression { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.OrderBy;

		public SqlOrderByExpression(OrderType orderType, Expression expression)
			: base(typeof(void))
		{
			this.OrderType = orderType;
			this.Expression = expression;
		}
	}
}
