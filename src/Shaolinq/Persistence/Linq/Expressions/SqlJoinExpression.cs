// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlJoinExpression
		: SqlBaseExpression
	{
		public SqlJoinType JoinType { get; set; }
		public Expression Left { get; }
		public Expression Right { get; }
		public Expression JoinCondition { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Join;

		public SqlJoinExpression(Type type, SqlJoinType joinType, Expression left, Expression right, Expression joinCondition)
			: base(type)
		{
			if (joinType != SqlJoinType.CrossApply && joinType != SqlJoinType.CrossJoin)
			{
				if (joinCondition == null)
				{
					throw new ArgumentNullException("joinCondition");
				}
			}

			this.JoinType = joinType; 
			this.Left = left;
			this.Right = right;
			this.JoinCondition = joinCondition;
		}
	}
}
