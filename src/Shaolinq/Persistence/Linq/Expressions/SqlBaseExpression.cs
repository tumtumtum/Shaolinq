// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public abstract class SqlBaseExpression
		: Expression
	{
		private readonly Type type; 
		public override Type Type { get { return type; } }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.Tuple; } }

		protected SqlBaseExpression(Type type)
		{
			this.type = type;
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
