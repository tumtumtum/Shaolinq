// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlJoinExpression
		: SqlBaseExpression
	{
		public SqlJoinType JoinType
		{
			get;
			set;
		}

		/// <summary>
		/// The left side of the join (can be an <see cref="SqlTableExpression"/>
		/// of <see cref="SqlSelectExpression"/>)
		/// </summary>
		public Expression Left { get; private set; }

		/// <summary>
		/// The right side of the join (can be an <see cref="SqlTableExpression"/>
		/// of <see cref="SqlSelectExpression"/>)
		/// </summary>
		public Expression Right { get; private set; }

		/// <summary>
		/// The condition of the join usually of the form SqlColumnExpression = SqlColumnExpression.
		/// </summary>
		public new Expression Condition
		{
			get;
			private set;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Join;
			}
		}

		public SqlJoinExpression(Type type, SqlJoinType joinType, Expression left, Expression right, Expression condition)
			: base(type)
		{
			this.JoinType = joinType;
			this.Left = left;
			this.Right = right;
			this.Condition = condition;
		}
	}
}
