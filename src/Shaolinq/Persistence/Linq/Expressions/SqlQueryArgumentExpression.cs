// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlQueryArgumentExpression
		: SqlBaseExpression
	{
		public int Index { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.QueryArgument;

		public SqlQueryArgumentExpression(Type type, int index)
			: base(type)
		{
			this.Index = index;
		}
	}
}
