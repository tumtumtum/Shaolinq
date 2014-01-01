// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateTypeExpression
		: SqlBaseExpression
	{
		public Expression SqlType { get; set; }
		public Expression AsExpression { get; set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.CreateType;
			}
		}

		public SqlCreateTypeExpression(Expression sqlType, Expression asExpression)
			: base(typeof(void))
		{
			this.SqlType = sqlType;
			this.AsExpression = asExpression;
		}
	}
}
