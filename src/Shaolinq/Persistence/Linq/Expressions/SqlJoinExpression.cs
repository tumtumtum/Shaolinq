// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlJoinExpression
		: SqlBaseExpression
	{
		public SqlJoinType JoinType { get; set; }
		public Expression Left { get; private set; }
		public Expression Right { get; private set; }
		public Expression JoinCondition { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.Join; } }

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
