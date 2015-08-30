// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlOrderByExpression
		: SqlBaseExpression
	{
		public OrderType OrderType { get; private set; }
		public Expression Expression { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.OrderBy; } }

		public SqlOrderByExpression(OrderType orderType, Expression expression)
			: base(typeof(void))
		{
			this.OrderType = orderType;
			this.Expression = expression;
		}
	}
}
