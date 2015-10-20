// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlDeleteExpression
		: SqlBaseExpression
	{
		public string Alias { get; }
		public SqlTableExpression Table { get; }
		public Expression Where { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Delete;

		public SqlDeleteExpression(SqlTableExpression table, string alias, Expression where)
			: base(typeof(void))
		{
			this.Where = where;
			this.Alias = alias;
			this.Table = table;
		}
	}
}
