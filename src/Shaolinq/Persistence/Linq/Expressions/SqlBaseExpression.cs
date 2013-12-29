// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public abstract class SqlBaseExpression
		: Expression
	{
		public override Type Type
		{
			get
			{
				return type;
			}
		}
		private readonly Type type;

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Tuple;
			}
		}

		protected SqlBaseExpression(Type type)
		{
			this.type = type;
		}

		public override string ToString()
		{
			return this.GetType().Name + ":" + new Sql92QueryFormatter(this, SqlQueryFormatterOptions.Default).Format().CommandText;
		}
	}
}
