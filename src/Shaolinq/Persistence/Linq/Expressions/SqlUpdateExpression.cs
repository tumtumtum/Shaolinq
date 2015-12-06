// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlUpdateExpression
		: SqlBaseExpression
	{
		public SqlTableExpression Table { get; }
		public Expression Where { get; }
		public IReadOnlyList<Expression> Assignments { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Update;

		public SqlUpdateExpression(SqlTableExpression table, IReadOnlyList<Expression> assignments, Expression where)
			: base(typeof(void))
		{
			this.Table = table;
			this.Assignments = assignments;
			this.Where = where;
		}
	}
}