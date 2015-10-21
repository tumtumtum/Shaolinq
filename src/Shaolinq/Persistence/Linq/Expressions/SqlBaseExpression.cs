// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public abstract class SqlBaseExpression
		: Expression
	{
		public override Type Type { get; }

		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Tuple;

		protected SqlBaseExpression(Type type)
		{
			this.Type = type;
		}

		public virtual string OriginalToString()
		{
			return base.ToString();
		}

		public override string ToString()
		{
			var optimized = SqlQueryProvider.Optimize(this, typeof(string));

			return new Sql92QueryFormatter(SqlQueryFormatterOptions.Default)
				.Format(optimized)
				.CommandText;
		}
	}
}
