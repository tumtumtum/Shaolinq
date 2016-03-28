// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlUpdateExpression
		: SqlBaseExpression
	{
		public Expression Source { get; }
		public Expression Where { get; }
		public IReadOnlyList<Expression> Assignments { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Update;

		public SqlUpdateExpression(Expression source, IReadOnlyList<Expression> assignments, Expression where)
			: base(typeof(void))
		{
			this.Source = source;
			this.Assignments = assignments;
			this.Where = where;
		}
	}
}