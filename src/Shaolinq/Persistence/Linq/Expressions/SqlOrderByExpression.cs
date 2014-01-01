// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	/// <summary>
	/// A pairing of an expression and an order type for use in a SQL Order By clause
	/// </summary>
	public class SqlOrderByExpression
		: SqlBaseExpression
	{
		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.OrderBy;
			}
		}

		public OrderType OrderType { get; private set; }
		public Expression Expression { get; private set; }

		internal SqlOrderByExpression(OrderType orderType, Expression expression)
			: base(typeof(void))
		{
			this.OrderType = orderType;
			this.Expression = expression;
		}
	}
}
