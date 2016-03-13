// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlUnionExpression
		: SqlAliasedExpression
	{
		public bool UnionAll { get; }
		public Expression Left { get; }
		public Expression Right { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Union;
		
		public SqlUnionExpression(Type type, string alias, Expression left, Expression right, bool unionAll)
			: base(type, alias)
		{
			this.Left = left;
			this.Right = right;
			this.UnionAll = unionAll;
		}
	}
}