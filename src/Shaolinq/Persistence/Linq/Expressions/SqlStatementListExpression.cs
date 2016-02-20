// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlStatementListExpression
		: SqlBaseExpression
	{
		public IReadOnlyList<Expression> Statements { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.StatementList;

		public SqlStatementListExpression(params Expression[] statements)
			: this(statements.ToReadOnlyCollection())
		{
		}

		public SqlStatementListExpression(IEnumerable<Expression> statements)
			: this(statements.ToReadOnlyCollection())
		{
		}
		
		public SqlStatementListExpression(IReadOnlyList<Expression> statements)
			: base(statements.Count > 0 ? statements[statements.Count - 1].Type : typeof(void))
		{
			this.Statements = statements;
		}
	}
}