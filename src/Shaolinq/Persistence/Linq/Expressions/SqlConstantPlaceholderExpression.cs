// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlConstantPlaceholderExpression
		: SqlBaseExpression
	{
		public int Index { get; set; }
		public ConstantExpression ConstantExpression { get; set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ConstantPlaceholder;
			}
		}

		public SqlConstantPlaceholderExpression(int index, ConstantExpression constantExpression)
			: base(constantExpression.Type)
		{
			this.Index = index;
			this.ConstantExpression = constantExpression;
		}
	}
}
